using System.Xml.Serialization;
using Roblox.Models.AbuseReport;

namespace Roblox.Dto.AbuseReport;

public class AbuseReportEntry
{
    public string id { get; set; }
    public long userId { get; set; }
    public long authorId { get; set; }
    public AbuseReportReason reportReason { get; set; }
    public AbuseReportStatus reportStatus { get; set; }
    public string reportMessage { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
}

[XmlRoot(ElementName = "report")]
public class InGameAbuseReportEntry
{

    [XmlElement(ElementName = "comment")]
    public string comment { get; set; }

    [XmlElement(ElementName = "messages")]
    public List<dynamic> messages { get; set; }

    [XmlAttribute(AttributeName = "userID")]
    public int userId { get; set; }

    [XmlAttribute(AttributeName = "placeID")]
    public int placeId { get; set; }

    [XmlAttribute(AttributeName = "gameJobID")]
    public string gameJobId { get; set; }
}
