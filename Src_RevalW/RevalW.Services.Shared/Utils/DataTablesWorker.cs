using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RevalW.Shared.Model;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.Services.Shared.Utils;

public class DataTablesWorker<InputClass, KeyType, OutputClass>(IServiceProvider serviceProvider) where InputClass : class
{
    private DbContext? _db;
    private DbContext Db => _db ??= serviceProvider.GetRequiredService<DbContext>();

    private IMapper? _mapper;
    private IMapper Mapper => _mapper ??= serviceProvider.GetRequiredService<IMapper>();

    public async Task<DataTablesResponse<OutputClass>> DoDataTablesSelect(IQueryable<InputClass> query, DTRequest request, IEnumerable<(string? jsonColumnName, IEnumerable<string>? jsonKeyNames)>? filter, Func<InputClass, Task<OutputClass>>? map = null)
    {
        try
        {
            // Base total records (applies existing filters supplied by caller)
            DataTablesResponse<OutputClass> ret;

            // No search requested -> simple path: sort, page, materialize normally
            (var needSearch, ret) = await NeedSearch(query, request, map);
            if (!needSearch) return ret;

            // If columnName/columnNames provided, do JSON search via raw SQL
            (var searchedByColumns, ret) = await TrySearchByColumns(query, request, filter, map);
            if (searchedByColumns) return ret;

            return await GetNonSearchedData(query, request, map);
        }
        catch (Exception)
        {
            return GetEmptyData(request.Draw);
        }
    }
    public async Task<DataTablesResponse<OutputClass>> DoDataTablesSelect(IQueryable<InputClass> query, DTRequest request, Func<InputClass, string, string, bool>? searchMethod, Func<InputClass, Task<OutputClass>>? map = null)
    {
        try
        {
            // Base total records (applies existing filters supplied by caller)
            DataTablesResponse<OutputClass> ret;

            // No search requested -> simple path: sort, page, materialize normally
            (var needSearch, ret) = await NeedSearch(query, request, map);
            if (!needSearch) return ret;

            // If a SearchMethod is provided, do in-memory filtering (fallback)
            (var searched, ret) = await TrySearch(query, request, searchMethod, map);
            if (searched) return ret;

            return await GetNonSearchedData(query, request, map);
        }
        catch (Exception)
        {
            return GetEmptyData(request.Draw);
        }
    }
    private async Task<(bool, DataTablesResponse<OutputClass>)> NeedSearch(IQueryable<InputClass> query, DTRequest request, Func<InputClass, Task<OutputClass>>? map)
    {
        if (!string.IsNullOrWhiteSpace(request.Search?.Value))
            return (true, null!);

        return (false, await GetNonSearchedData(query, request, map));
    }
    private async Task<(bool, DataTablesResponse<OutputClass>)> TrySearch(IQueryable<InputClass> query, DTRequest request, Func<InputClass, string, string, bool>? searchMethod, Func<InputClass, Task<OutputClass>>? map)
    {
        if (searchMethod is null)
            return (false, null!);

        var totalRecords = await query.CountAsync();
        var searchValue = request.Search!.Value!;
        // materialize filtered set in-memory then apply func
        var all = query.ToList();
        var lower = searchValue.ToLower();
        var filtered = all.Where(i => searchMethod(i, searchValue, lower)).ToList();
        var filteredCount = filtered.Count;
        var paged = filtered.Skip(request.Start).Take(request.Length).ToList();
        var mapped = await MapResults(paged, map);

        return (true, new()
        {
            draw = request.Draw,
            recordsTotal = totalRecords,
            recordsFiltered = filteredCount,
            data = mapped
        });
    }
    private async Task<(bool, DataTablesResponse<OutputClass>)> TrySearchByColumns(IQueryable<InputClass> query, DTRequest request, IEnumerable<(string? jsonColumnName, IEnumerable<string>? jsonKeyNames)>? filter, Func<InputClass, Task<OutputClass>>? map)
    {
        if (filter is null || !filter.Any())
            return (false, null!);
        if (filter.Any(f => string.IsNullOrEmpty(f.jsonColumnName)))
            return (false, null!);

        var totalRecords = await query.CountAsync();
        var searchValue = request.Search!.Value!;

        // Obtain ordered list of ids from original query (apply requested sort)
        var orderedQuery = AddSort(query, request);
        // We need the primary key name; assume property named "Id"
        var idList = orderedQuery.Select(e => EF.Property<KeyType>(e, "Id")).ToList();
        if (idList.Count == 0)
            return (true, new()
            {
                draw = request.Draw,
                recordsTotal = 0,
                recordsFiltered = 0,
                data = Enumerable.Empty<OutputClass>()
            });

        // Build table name
        var et = Db.Model.FindEntityType(typeof(InputClass)) ?? throw new InvalidOperationException($"Cannot find entity type for {typeof(InputClass).FullName}");
        var tbl = et.GetTableName();
        var schema = et.GetSchema();
        var fullTbl = string.IsNullOrWhiteSpace(schema) ? $"\"{tbl}\"" : $"\"{schema}\".\"{tbl}\"";
        long filteredRecords = 0;

        // Build IN-list (ids originate from DB so safe to inline)
        string idsCsv;
        if (typeof(KeyType) != typeof(string))
            idsCsv = string.Join(",", idList);
        else
        {
            var strIds = idList.Cast<string>().Select(id => $"'{id.Replace("'", "''")}'");
            idsCsv = string.Join(",", strIds);
        }

        try
        {
            // Compute filteredCount
            var jsonWhere = FetchJsonQueryPart(filter);
            var jsonParams = FetchJsonParameters(filter, searchValue);

            var countSql = $"SELECT COUNT(*) FROM {fullTbl} t WHERE t.\"Id\" IN ({idsCsv}) AND ({jsonWhere});";

            filteredRecords = await ExecuteScalarCount(countSql, jsonParams);
        }
        catch
        {
            filteredRecords = 0;
        }
        // Fetch paged rows using raw SQL. Try to preserve ordering from ordered id list.
        var start = request.Start;
        var len = request.Length;
        List<InputClass> results;

        try
        {
            // Filtered fetch
            var jsonWhere = FetchJsonQueryPart(filter);
            var jsonParams = FetchJsonParameters(filter, searchValue);
            string keyType;
            if (typeof(KeyType) != typeof(string))
                keyType = "bigint";
            else
                keyType = "text";
            var fetchSql =
                $"SELECT * FROM {fullTbl} t WHERE t.\"Id\" = ANY(ARRAY[{idsCsv}]::{keyType}[]) AND ({jsonWhere}) ORDER BY array_position(ARRAY[{idsCsv}]::{keyType}[], t.\"Id\") LIMIT {len} OFFSET {start};";

            results = await ExecuteReaderQuery(fetchSql, jsonParams);
        }
        catch
        {
            results = [];
        }

        var mappedResults = await MapResults(results, map);

        return (true, new()
        {
            draw = request.Draw,
            recordsTotal = totalRecords,
            recordsFiltered = filteredRecords,
            data = mappedResults
        });
    }
    private static IQueryable<InputClass> AddPaging(IQueryable<InputClass> query, DTRequest request)
    {
        return query.Skip(request.Start).Take(request.Length);
    }
    private static IQueryable<InputClass> AddSort(IQueryable<InputClass> query, DTRequest request)
    {
        if (request.Order is null || request.Order.Count == 0) return query;

        var order = request.Order.First();
        var columnName = request.Columns?[order.Column].Data;
        if (string.IsNullOrEmpty(columnName)) return query;

        return order.Dir == "asc"
            ? query.OrderByDynamic(columnName, true)
            : query.OrderByDynamic(columnName, false);
    }
    private async Task<long> ExecuteScalarCount(string sql, IEnumerable<NpgsqlParameter> parameters)
    {
        try
        {
            var conn = Db.Database.GetDbConnection();
            await conn.OpenAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters.ToArray());
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt64(result);
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        catch
        {
            return 0;
        }
    }
    private async Task<List<InputClass>> ExecuteReaderQuery(string sql, IEnumerable<NpgsqlParameter> parameters)
    {
        try
        {
            return await Db.Set<InputClass>().FromSqlRaw(sql, parameters.ToArray()).ToListAsync();
        }
        catch
        {
            return [];
        }
    }
    private async Task<DataTablesResponse<OutputClass>> GetNonSearchedData(IQueryable<InputClass> query, DTRequest request, Func<InputClass, Task<OutputClass>>? map)
    {
        var totalRecords = await query.CountAsync();
        var qSorted = AddSort(query, request);
        var qPaged = AddPaging(qSorted, request);
        var page = qPaged.ToList();

        var mapped = await MapResults(page, map);

        return new()
        {
            draw = request.Draw,
            recordsTotal = totalRecords,
            recordsFiltered = totalRecords,
            data = mapped
        };
    }
    private async Task<IEnumerable<OutputClass>> MapResults(IEnumerable<InputClass> source, Func<InputClass, Task<OutputClass>>? map)
    {
        if (map is null)
            return Mapper.Map<IEnumerable<OutputClass>>(source);
        else
        {
            var mappedList = new List<OutputClass>();
            foreach (var item in source)
            {
                var mappedItem = await map(item);
                mappedList.Add(mappedItem);
            }
            return mappedList;
        }
    }
    private static DataTablesResponse<OutputClass> GetEmptyData(int draw)
    {
        return new()
        {
            draw = draw,
            recordsTotal = 0,
            recordsFiltered = 0,
            data = Enumerable.Empty<OutputClass>()
        };
    }

    /// <summary>
    /// Build JSON conditions (simple ILIKE {search}%)
    /// </summary>
    /// <param name="columnNames"></param>
    /// <param name="columnName"></param>
    /// <returns></returns>
    private static string FetchJsonQueryPart(IEnumerable<(string? jsonColumnName, IEnumerable<string>? jsonKeyNames)> filter)
    {
        var columnIndex = 0;
        List<string> conditions = [];
        foreach (var (jsonColumnName, jsonKeyNames) in filter)
        {
            if (string.IsNullOrWhiteSpace(jsonColumnName)) continue;
            var column = $"\"{jsonColumnName}\"";
            if (jsonKeyNames is null)
            {
                conditions.Add($"{column} ILIKE @colVal{columnIndex++}");
                continue;
            }
            foreach (var key in jsonKeyNames ?? [])
            {
                column = $"\"{jsonColumnName}\"->>'{key}'";
                conditions.Add($"{column} ILIKE @colVal{columnIndex++}");
            }
        }
        return string.Join(" OR ", conditions);
    }

    /// <summary>
    /// Build query parameters
    /// </summary>
    /// <param name="columnNames"></param>
    /// <param name="searchValue"></param>
    /// <returns></returns>
    private static List<NpgsqlParameter> FetchJsonParameters(IEnumerable<(string? jsonColumnName, IEnumerable<string>? jsonKeyNames)> filter, string searchValue)
    {
        var columnIndex = 0;
        var safeSearch = searchValue.Replace("'", "''") + "%";
        List<NpgsqlParameter> parameters = [];
        foreach (var (jsonColumnName, jsonKeyNames) in filter)
        {
            if (string.IsNullOrWhiteSpace(jsonColumnName)) continue;
            if (jsonKeyNames is null)
            {
                parameters.Add(new NpgsqlParameter($"colVal{columnIndex++}", safeSearch));
                continue;
            }
            foreach (var key in jsonKeyNames ?? [])
            {
                parameters.Add(new NpgsqlParameter($"colVal{columnIndex++}", safeSearch));
            }
        }
        return parameters;
    }
}