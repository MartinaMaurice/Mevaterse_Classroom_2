using ExitGames.Client.Photon;
using UnityEngine;

public static class PhotonCustomTypes
{
    public static void RegisterCustomTypes()
    {
        PhotonPeer.RegisterType(typeof(Color32), (byte)'C', SerializeColor32, DeserializeColor32);
    }

    private static byte[] SerializeColor32(object obj)
    {
        Color32 color = (Color32)obj;
        return new byte[] { color.r, color.g, color.b, color.a };
    }

    private static object DeserializeColor32(byte[] data)
    {
        return new Color32(data[0], data[1], data[2], data[3]);
    }
}
