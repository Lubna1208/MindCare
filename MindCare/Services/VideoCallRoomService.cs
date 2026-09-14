using System.Security.Cryptography;
using System.Text;

namespace MindCare.Services;

public interface IVideoCallRoomService
{
    bool IsConfigured { get; }

    string Domain { get; }

    string GetRoomName(int appointmentId);
}

/// <summary>Derives opaque, stable Jitsi room names without storing them in the database.</summary>
public sealed class VideoCallRoomService : IVideoCallRoomService
{
    private readonly byte[]? _roomSecret;

    public VideoCallRoomService(IConfiguration configuration, ILogger<VideoCallRoomService> logger)
    {
        var secret = configuration["VideoCall:RoomSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            logger.LogError("Video calling is unavailable because VideoCall:RoomSecret is not configured.");
        }
        else
        {
            _roomSecret = Encoding.UTF8.GetBytes(secret);
        }

        Domain = configuration["VideoCall:Domain"]?.Trim() switch
        {
            { Length: > 0 } configuredDomain => configuredDomain,
            _ => "meet.jit.si"
        };
    }

    public bool IsConfigured => _roomSecret is not null;

    public string Domain { get; }

    public string GetRoomName(int appointmentId)
    {
        if (_roomSecret is null)
        {
            throw new InvalidOperationException("Video calling has not been configured.");
        }

        var payload = Encoding.UTF8.GetBytes($"mindcare-appointment:{appointmentId}");
        var hash = HMACSHA256.HashData(_roomSecret, payload);
        return "mindcare-" + Convert.ToHexString(hash).ToLowerInvariant()[..32];
    }
}
