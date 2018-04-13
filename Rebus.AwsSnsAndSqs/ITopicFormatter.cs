namespace Rebus.AwsSnsAndSqs
{
    public interface ITopicFormatter
    {
        string FormatTopic(string topic);
    }
}
