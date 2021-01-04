namespace MsGlossaryApp.DataModel
{
    public interface IEqual
    {
        bool IsEqualTo(IEqual other);
    }
}