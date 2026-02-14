namespace MyTestFramework
{
    public interface IContext
    {
        void SetData(string key, object value);
        object GetData(string key);
        bool ContainsData(string key);
        void RemoveData(string key);
    }
}

