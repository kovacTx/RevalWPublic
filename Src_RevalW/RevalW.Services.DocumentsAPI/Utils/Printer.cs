using DisplayValue;
using ErrorResult;
using RevalM.SharedStorage.LSData;
using System.Xml;

namespace RevalW.Services.DocumentsAPI.Utils;

public class Printer()
{
    public void FillAndPrintXML(string targetName, Creator.DataHolder data)
    {
        if (string.IsNullOrEmpty(targetName)) return;
        var xmlSetting = new XmlWriterSettings
        {
            ConformanceLevel = ConformanceLevel.Auto,
            Indent = true,
            IndentChars = " "
        };

        using var writer = XmlWriter.Create(targetName, xmlSetting);
        writer.WriteStartElement("document");
        WriteData(writer, data);
        //var printSgnRet = this.PrintSign(writer, item.EmailMeral, item.OrderItemNumber);
        //if (!printSgnRet.IsOk)
        //{
        // english string
        //    Logger.Fatal($"neexistuje podpis pre užívateľa {item.EmailMeral} ({printSgnRet.Message})");
        //    return printSgnRet;
        //}

        writer.WriteEndElement();
        writer.Flush();
    }
    private void WriteData(XmlWriter writer, Creator.DataHolder data)
    {
        foreach (var innerItem in data.Inner)
        {
            PrintDataHolder(writer, innerItem);
        }
    }
    private void PrintDataHolder(XmlWriter writer, Creator.DataHolder toPrint)
    {
        writer.WriteStartElement(EscapeText(toPrint.Name));
        PrintDataHolderContent(writer, toPrint);
        writer.WriteEndElement();
    }
    private void PrintDataHolderContent(XmlWriter writer, Creator.DataHolder toPrint)
    {
        PrintItem(writer, toPrint.Item);
        if (toPrint.Inner is null) return;
        foreach (var innerItem in toPrint.Inner) PrintDataHolder(writer, innerItem);
    }
    private static bool PrintItem(XmlWriter writer, ValueWrapper? item)
    {
        if (item is null) return false;
        try
        {
            writer.WriteStartElement("Value");
            PrintWrapper(writer, item);
            return true;
        }
        finally
        {
            writer.WriteEndElement();
        }
    }
    private static void PrintWrapper(XmlWriter writer, ValueWrapper? wrapper)
    {
        if (wrapper is null) return;
        PrintValue(writer, wrapper.MyValue);
    }
    private static bool PrintValue(XmlWriter writer, object? value)
    {
        if (value is null) return false;
        if (value is bool boolean)
        {
            writer.WriteString(boolean.ToString().ToLower());
            return true;
        }
        if (value is string str)
        {
            // todo: when i have number with units, i think i geenerate dispValuewrapper with string children, that should be the unit, it should fetch the unit converter and print value with unit and prefix 
            writer.WriteString(str);
            return true;
        }
        if (value is double doubleValue)
        {
            writer.WriteString(doubleValue.ToString());
            return true;
        }
        if (value is int intValue)
        {
            writer.WriteString(intValue.ToString());
            return true;
        }
        if (value is decimal decimalValue)
        {
            writer.WriteString(decimalValue.ToString());
            return true;
        }
        if (value is DispValue dispValue)
        {
            PrintDispValue(writer, dispValue);
            return true;
        }
        if (value is ErrorLevel errorLevel)
        {
            writer.WriteElementString("Level", errorLevel.ErrLevel.ToString());
            writer.WriteElementString("Message", errorLevel.Message);
            return true;
        }
        if (value is DateTime dateTime)
        {
            writer.WriteString(dateTime.ToString("dd.MM.yyyy"));
            return true;
        }
        if (value is TimeSpan time)
        {
            writer.WriteString(time.ToString("hh:mm"));
            return true;
        }
        //if (value is DispValueToUserUnitConverter converter)
        //{
        //    string valueToWrite;
        //    if (parentValue is not DispValue dv) valueToWrite = converter.Shortcut;
        //    else valueToWrite = converter.Convert(dv, false, true);
        //    writer.WriteString(valueToWrite);
        //}
        //if (value is Chart chart)
        //{
        //    var indexes = this.variablePathBuilder.Explode(chart.Address);
        //    foreach (var index in indexes.Parts)
        //    {
        //        index.IndexName = string.Empty;
        //    }
        //    var chartName = Path.Combine(this.cSVDirInProgress, this.EscapeFileName(indexes.ToString() + ".png"));
        //    PlotHelper.Singleton.PrintPlot(chart, chartName, chart.PrintWidth, chart.PrintHeight);
        //    chartName = chartName.Replace(@"\\", "/");
        //    chartName = chartName.Replace(@"\", "/");
        //    writer.WriteString("url('file:" + chartName + "')");
        //    this.TempFiles.Add(chartName);
        //    return true;
        //}
        return false;
    }
    private static void PrintDispValue(XmlWriter writer, DispValue dispValue)
    {
        var stringValue = dispValue.ToString();
        writer.WriteString(ConvertLocalisationOfDouble(stringValue));
    }
    private static string ConvertLocalisationOfDouble(string value)
        => value.Replace('.', ',');
    //private void PrintEtalons(XmlWriter writer, OrderItemBase orderItem)
    //{
    //    writer.WriteStartElement("Etalons");
    //    if (orderItem.UsedEtalons is object)
    //    {
    //        foreach (var item in orderItem.UsedEtalons)
    //        {
    //            if (item is null) continue;
    //            writer.WriteStartElement("EtalonDoc");
    //            writer.WriteElementString("Name", item.Device.Name);
    //            writer.WriteElementString("ShortName", item.ShortName);
    //            writer.WriteElementString("ProductNumber", item.Device.ProductNumber);
    //            writer.WriteElementString("Manufacturer", item.Manufacturer);
    //            writer.WriteElementString("CokVydal", item.CokVydal);
    //            writer.WriteElementString("CokNumber", item.CokNumber);
    //            writer.WriteElementString("RegNumber", item.RegNumber);
    //            writer.WriteElementString("Type", item.Type);
    //            writer.WriteEndElement();
    //        }
    //    }
    //    writer.WriteEndElement();
    //}
    //private void PrintMeasuringDevices(XmlWriter writer, OrderItemBase orderItem)
    //{
    //    writer.WriteStartElement("MeasuringDevices");
    //    if (orderItem.UsedMeasuringDevices is object)
    //    {
    //        foreach (var item in orderItem.UsedMeasuringDevices)
    //        {
    //            if (item is null) continue;
    //            writer.WriteStartElement("MeasuringDeviceDoc");
    //            writer.WriteElementString("Name", item.Device.Name);
    //            writer.WriteElementString("ShortName", item.ShortName);
    //            writer.WriteElementString("ProductNumber", item.Device.ProductNumber);
    //            writer.WriteElementString("Manufacturer", item.Manufacturer);
    //            writer.WriteElementString("CokVydal", item.CokVydal);
    //            writer.WriteElementString("CokNumber", item.CokNumber);
    //            writer.WriteElementString("RegNumber", item.RegNumber);
    //            writer.WriteElementString("Type", item.Type);
    //            writer.WriteEndElement();
    //        }
    //    }
    //    writer.WriteEndElement();
    //}
    //private void PrintHelpingDevices(XmlWriter writer, OrderItemBase orderItem)
    //{
    //    writer.WriteStartElement("HelpingDevices");
    //    if (orderItem.UsedHelpingDevices is object)
    //    {
    //        foreach (var item in orderItem.UsedHelpingDevices)
    //        {
    //            if (item is null) continue;
    //            writer.WriteStartElement("HelpingDeviceDoc");
    //            writer.WriteElementString("Name", item.Device.Name);
    //            writer.WriteElementString("ShortName", item.ShortName);
    //            writer.WriteElementString("ProductNumber", item.Device.ProductNumber);
    //            writer.WriteElementString("Manufacturer", item.Manufacturer);
    //            writer.WriteElementString("CokVydal", item.CokVydal);
    //            writer.WriteElementString("CokNumber", item.CokNumber);
    //            writer.WriteElementString("RegNumber", item.RegNumber);
    //            writer.WriteElementString("Type", item.Type);
    //            writer.WriteEndElement();
    //        }
    //    }
    //    writer.WriteEndElement();
    //}
    private static void PrintStandard(XmlWriter writer, string name, StandardWrapper standard)
    {
        if (standard is null) return;
        writer.WriteStartElement(name);
        writer.WriteElementString("Name", standard.Value);
        writer.WriteEndElement();
    }
    private static void PrintName(XmlWriter writer, ValueWrapper item)
    {
        writer.WriteStartElement("Name");
        writer.WriteString(item.Name);
        writer.WriteEndElement();
    }
    private static string EscapeText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace('#', '_').Replace('~', '-');
        var fiirstChar = text[0];
        if (fiirstChar >= '0' && fiirstChar <= '9') text = '_' + text;
        return text;
    }
}
