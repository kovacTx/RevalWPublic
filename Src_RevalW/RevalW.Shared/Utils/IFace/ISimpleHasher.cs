namespace RevalW.Shared.Utils.IFace;

public interface ISimpleHasher
{
    string ComputeHash(string rawData);
    string EncodePath(string path);
    string DecodePath(string encoded);
}