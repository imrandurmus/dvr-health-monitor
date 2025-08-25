namespace DvrWorker.Configurations;

public sealed class MongoOptions
{
	public string ConnectionString { get; set; } = "mongodb://localhost:27018";
	public string Database { get; set; } = "DvrHealth";
}
