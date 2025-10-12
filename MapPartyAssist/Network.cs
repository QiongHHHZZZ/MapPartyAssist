using Dalamud.Hooking;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Network;
using Lumina;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MapPartyAssist {
    public class Network {
        
           internal static unsafe void Init()
    {

        ReceivePacketInternalHook ??= Svc.Hook.HookFromAddress<ReceivePacketInternalDelegate>(
            GetVFuncByName(PacketDispatcher.StaticVirtualTablePointer, "OnReceivePacket"),
            ReceivePacketInternalDetour
        );
        ReceivePacketInternalHook.Enable();
    }

    internal static void Dispose()
    {
        ReceivePacketInternalHook?.Dispose();
    }

   
    private static void OnLogMessageShow(uint logId, uint param1, uint param2, uint param3)
    {
        switch (logId)
        {
            case  0:
            {
                break;
            }
            
        }
        Svc.Log.Verbose($"MessageId:{logId}");
    }

    #region Network Hooks

    private const int PacketLength = 64;

    private unsafe delegate void ReceivePacketInternalDelegate(
        PacketDispatcher* dispatcher,
        uint targetID,
        byte* packet
    );
    private static Hook<ReceivePacketInternalDelegate>? ReceivePacketInternalHook;

    public static unsafe nint GetVFuncByName<T>(T* vtablePtr, string fieldName)
        where T : unmanaged
    {
        var vtType = typeof(T);
        var fi = vtType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (fi == null)
            throw new MissingFieldException(vtType.FullName, fieldName);

        var offAttr = fi.GetCustomAttribute<FieldOffsetAttribute>();
        if (offAttr == null)
            throw new InvalidOperationException($"Field {fieldName} has no FieldOffset");

        var offset = offAttr.Value;

        return *(nint*)((byte*)vtablePtr + offset);
    }

    private static unsafe void ReceivePacketInternalDetour(
        PacketDispatcher* dispatcher,
        uint targetId,
        byte* packet
    )
    {
            DetectMessage(packet);
       

        ReceivePacketInternalHook.Original(dispatcher, targetId, packet);
    }

    private static unsafe void DetectMessage(byte* packet)
    {
        // 检查包类型标识符 (0x0014)
        ushort packetType = *(ushort*)packet;
        if (packetType != 0x0014)
            return;

        // 检查特征值  (偏移16)
        uint featureValue = *(uint*)(packet + 16);

        uint logId = *(uint*)(packet + 20);
        uint param1 = *(uint*)(packet + 24);
        uint param2 = *(uint*)(packet + 28);
        uint param3 = *(uint*)(packet + 30);

        if (featureValue is not (0x00000205 or 0x00150001))
            return;
        OnLogMessageShow(logId, param1, param2, param3);

        // 记录日志
        Svc.Log.Verbose($"Packet Received: {FormatBytesSimple(packet, PacketLength)}");
    }

    public static unsafe string FormatBytesSimple(byte* dataPtr, int lengthInBytes)
    {
        if (dataPtr == null || lengthInBytes <= 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(lengthInBytes * 3); // 预估容量

        // 计算可以完整读取的32位值的数量
        int fullUintCount = lengthInBytes / 4;

        // 处理所有完整的32位值
        for (int i = 0; i < fullUintCount; i++)
        {
            // 以小端序读取32位值
            uint value = *(uint*)(dataPtr + (i * 4));

            // 格式化为十六进制
            sb.Append($"{value:X8}");

            // 每1个32位值添加一个分隔符（最后一个不加）
            if (i + 1 < fullUintCount)
            {
                sb.Append('|');
            }
        }

        // 处理剩余的不足4字节的部分（如果有）
        int remainingBytes = lengthInBytes % 4;
        if (remainingBytes > 0)
        {
            // 如果前面已经有数据，添加分隔符
            if (fullUintCount > 0)
            {
                sb.Append('|');
            }

            // 处理剩余字节
            uint remainingValue = 0;
            for (int i = 0; i < remainingBytes; i++)
            {
                // 按小端序构建剩余值
                remainingValue |= (uint)(dataPtr[fullUintCount * 4 + i] << (i * 8));
            }

            // 根据剩余字节数决定显示多少位十六进制
            string format = $"X{remainingBytes * 2}";
            sb.Append(remainingValue.ToString(format));
        }

        return sb.ToString();
    }

    #endregion
    }
}