namespace Resplado.SqLite;
public interface ISqLiteRepository
{
    bool ExistTable();
    void CreateTableMonthlyLog();
    void CreateTableWeeklyLog();
    void CreateTableDaily();
    void Insert(string tabla, string fecha, string path);
}

