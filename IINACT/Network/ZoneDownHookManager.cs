using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using Unscrambler;
using Unscrambler.Constants;
using Unscrambler.Unscramble;
using Unscrambler.Unscramble.Versions;

namespace IINACT.Network;

public unsafe class ZoneDownHookManager : IDisposable
{
	private const string GenericDownSignature = "E8 ?? ?? ?? ?? 4C 8B 4F 10 8B 47 1C 45";
    private const string OnPacketRecieveSignatureTC = "48 89 5C 24 20 55 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 E0 EF FF FF B8 20 11 00 00 E8 AE E6 5C 00 48 2B E0 48 8B 05 AC 59 09 01 48 33 C4 48 89 85 18 10 00 00 45 0F B7 78 02";
    private const ulong OnPacketRecieveAddressTC = 0x14168BD10;
    private const ulong DeriveAddressTC = 0x141691010;
    private const string OpcodeKeyTableSignature = "?? ?? ?? 2B C8 ?? 8B ?? 8A ?? ?? ?? ?? 41 81";
    private readonly int[] opcodeKeyTable;
    private readonly byte[] keys = new byte[3];
    
    private readonly INotificationManager notificationManager;
	private delegate nuint DownPrototype(byte* data, byte* a2, nuint a3, nuint a4, nuint a5);
	
	private readonly Hook<DownPrototype> zoneDownHook;
    
	private readonly SimpleBuffer buffer;
    
    private readonly VersionConstants versionConstants;
    private readonly IUnscrambler unscrambler;

	public ZoneDownHookManager(
        INotificationManager notificationManager,
		IGameInteropProvider hooks)
    {
        this.notificationManager = notificationManager;
		buffer = new SimpleBuffer(1024 * 1024);
        var multiScanner = new MultiSigScanner();
        var moduleBase = multiScanner.Module.BaseAddress;
        
        var version = GetRunningGameVersion();
        var isGlobal = Machina.FFXIV.Headers.Opcodes.OpcodeManager.Instance.GameRegion == Machina.FFXIV.GameRegion.Global;
        if (isGlobal && VersionConstants.Constants.ContainsKey(version))
        {
            versionConstants = VersionConstants.ForGameVersion(version);
            unscrambler = UnscramblerFactory.ForGameVersion(version);
        }
        else
        {
            // TC 7.1 seems dosen't have OpcodeKeyTable @ OnPacketRecieve Switch content
            // Struct may fit for Global 7.2
            Plugin.Log.Warning("[ZoneDownHookManager] Unscrambler7.2 TC");
            versionConstants = new VersionConstants
            {
                GameVersion = GetRunningGameVersion(),
                InitZoneOpcode = 0x227,                             //FFXIVOpcodes/FFXIVOpcodes/Ipcs_tw.cs
                UnknownObfuscationInitOpcode = 0x0,
                OpcodeKeyTableOffset = 0,
                OpcodeKeyTableSize = 0,
                TableOffsets = [0x2162570, 0x21755F0, 0x2179560],
                TableRadixes = [0xCB, 0x29, 0xE9],
                TableSizes = [96 * 0xCB, 99 * 0x29, 128 * 0xE9],
                MidTableOffset = 0x2162350,
                MidTableSize = 0x44 * 8,
                DayTableOffset = 0x2196760,
                DayTableSize = (0xE + 1) * 4,
                ObfuscatedOpcodes = new Dictionary<string, int>
                {
                    { "PlayerSpawn", 0x1C5 },                       //FFXIVOpcodes/FFXIVOpcodes/Ipcs_tw.cs
                    { "NpcSpawn", 0x27D },                          //FFXIVOpcodes/FFXIVOpcodes/Ipcs_tw.cs
                    { "NpcSpawn2", 0x1E2 },                         //Does not seem to be sent to the client

                    { "ActionEffect01", 0x1F9 },                    //Effect
                    { "ActionEffect08", 0x239 },                    //AoeEffect8
                    { "ActionEffect16", 0x33A },                    //AoeEffect16
                    { "ActionEffect24", 0xAC },                     //AoeEffect24
                    { "ActionEffect32", 0x9C },                     //AoeEffect32
                    { "StatusEffectList", 0x317 },                  //FFXIVOpcodes/FFXIVOpcodes/Ipcs_tw.cs
                    { "StatusEffectList3", 0x1CC },                 //FFXIVOpcodes/FFXIVOpcodes/Ipcs_tw.cs

                    { "Examine",0x67 },                             //FFXIVOpcodes/FFXIVOpcodes/Ipcs_tw.cs
                    { "UpdateGearset", 0 },                         //ModelEquip
                    { "UpdateParty", 0xF5 },
                    { "ActorControl", 0x191 },                      //FFXIVOpcodes/FFXIVOpcodes/Ipcs_tw.cs
                    { "ActorCast", 0x23B },                         //FFXIVOpcodes/FFXIVOpcodes/Ipcs_tw.cs
                    { "ActorControlSelf", 0x03C3 },                 //FFXIVOpcodes/FFXIVOpcodes/Ipcs_tw.cs

                    { "UnknownEffect01", 0 },                       //Does not seem to be sent to the client, EventAction8->EventAction16
                    { "UnknownEffect16", 0 },                       //Does not seem to be sent to the client, EventAction16->EventAction32
                    { "ActionEffect02", 0 },                        //Does not seem to be sent to the client, EventFinish64->EventFinish128
                    { "ActionEffect04", 0 }                         //Does not seem to be sent to the client, EventFinish128->EventFinish255
                }
            };

            unscrambler = new Unscrambler72();
            unscrambler.Initialize(versionConstants);
        }
        
        var rxPtrs = multiScanner.ScanText(GenericDownSignature, 3);
		zoneDownHook = hooks.HookFromAddress<DownPrototype>(rxPtrs[2], ZoneDownDetour);

		Enable();
    }
    
    private static bool IsModulePointer(ReadOnlySpan<byte> memory, int offset, nint moduleBase, long moduleSize)
    {
        if (offset + 8 > memory.Length) return false;
        var ptr = BitConverter.ToUInt64(memory.Slice(offset));
        return ptr >= (ulong)moduleBase && ptr < (ulong)(moduleBase + moduleSize);
    }

	public void Enable()
    {
        UpdateKeys();
		zoneDownHook?.Enable();
	}
    
    private void UpdateKeys()
    {
        var dispatcher = PacketDispatcher.GetInstance();
        
        if (dispatcher != null)
        {
            var gameRandom = dispatcher->GameRandom;
            var packetRandom = dispatcher->LastPacketRandom;
            byte key0 = 0, key1 = 0, key2 = 0;
            
            var obfuscationKeysLoaded = dispatcher->Key0 >= gameRandom + packetRandom;

            if (obfuscationKeysLoaded)
            {
                key0 = (byte)(dispatcher->Key0 - gameRandom - packetRandom);
                key1 = (byte)(dispatcher->Key1 - gameRandom - packetRandom);
                key2 = (byte)(dispatcher->Key2 - gameRandom - packetRandom);	
            }
            
            if (key0 != keys[0] || key1 != keys[1] || key2 != keys[2])
            {
                keys[0] = key0;
                keys[1] = key1;
                keys[2] = key2;    
                Plugin.Log.Debug($"[UpdateKeys] keys {dispatcher->Key0}, {dispatcher->Key1}, {dispatcher->Key2}");
                Plugin.Log.Debug($"[UpdateKeys] game random {dispatcher->GameRandom}, packet random {dispatcher->LastPacketRandom}");
            }
        }
        else
        {
            Plugin.Log.Warning("[UpdateKeys] Dispatcher was null, so not initializing keys");
        }
    }
	
	public void Disable()
	{
		zoneDownHook?.Disable();
	}
	
	public void Dispose()
	{
		Disable();
		zoneDownHook?.Dispose();
	}
    
    private void SendNotification(string content)
    {
        notificationManager.AddNotification(new Notification
        {
            Content = content,
            Title = "IINACT", 
        });
        Plugin.Log.Debug($"[SendNotification] {content}");
    }
    
    private nuint ZoneDownDetour(byte* data, byte* a2, nuint a3, nuint a4, nuint a5)
    {
	    var ret = zoneDownHook.Original(data, a2, a3, a4, a5);

	    var packetOffset = *(uint*)(data + 28);
	    if (packetOffset != 0) return ret;
	    
	    try
	    {
		    PacketsFromFrame((byte*) *(nint*)(data + 16));
	    }
	    catch (Exception e)
	    {
            Plugin.Log.Error(e, "[PacketsFromFrame] Error!");
	    }

        return ret;
    }
    
    private void PacketsFromFrame(byte* framePtr)
    {
        if ((nuint)framePtr == 0)
        {
            Plugin.Log.Error("null ptr");
            return;
        }
        
        var headerSize = Unsafe.SizeOf<FrameHeader>();
        var headerSpan = new Span<byte>(framePtr, headerSize);
        var header = headerSpan.Cast<byte, FrameHeader>();
        var span = new Span<byte>(framePtr, (int)header.TotalSize);
        var data = span.Slice(headerSize, (int)header.TotalSize - headerSize);
        
        // Compression
        if (header.Compression != CompressionType.None)
        {
            SendNotification($"A frame was compressed.");
            return;
        }
        
        GameServerTime.SetLastServerTimestamp(header.TimeValue);
        
        // Deobfuscation
        var offset = 0;
        for (var i = 0; i < header.Count; i++)
        {
	        var pktHdrSize = Unsafe.SizeOf<PacketElementHeader>();
            var pktHdrSlice = data.Slice(offset, pktHdrSize);
            var pktHdr = pktHdrSlice.Cast<byte, PacketElementHeader>();
            var pktData = data.Slice(offset + pktHdrSize, (int)pktHdr.Size - pktHdrSize);
            var pktOpcode = OpcodeUtility.GetOpcodeFromPacketAtIpcStart(pktData);
            var needsDeobfuscation = versionConstants.ObfuscatedOpcodes.ContainsValue(pktOpcode);
            
            buffer.Clear();
            buffer.Write(pktHdrSlice);

            if (needsDeobfuscation)
            {
                UpdateKeys();
                var pos = buffer.Size;
                buffer.Write(pktData);
                var slice = buffer.Get(pos, pktData.Length);
                unscrambler.Unscramble(slice, keys[0], keys[1], keys[2], opcodeKeyTable);
            }
            else
            {
                buffer.Write(pktData);    
            }
            
            EnqueueToMachina(buffer.GetBuffer());
            
            offset += (int)pktHdr.Size;
        }
    }

    private static void EnqueueToMachina(ReadOnlySpan<byte> data)
    {
        var queue = Machina.FFXIV.Dalamud.DalamudClient.MessageQueue;
        queue?.Enqueue((GameServerTime.LastSeverTimestamp, data.ToArray()));
    }
    
    private static string GetRunningGameVersion()
    {
        var path = Environment.ProcessPath!;
        var parent = Directory.GetParent(path)!.FullName;
        var ffxivVerFile = Path.Combine(parent, "ffxivgame.ver");
        return File.Exists(ffxivVerFile) ? File.ReadAllText(ffxivVerFile) : "0000.00.00.0000.0000";
    }
    
    public static VersionConstants GetFallbackVersionConstant(uint opcodeKeyTableOffset, int opcodeKeyTableSize)
    {
        var opcodes = Machina.FFXIV.Headers.Opcodes.OpcodeManager.Instance.CurrentOpcodes;
        return new VersionConstants
        {
            GameVersion = GetRunningGameVersion(),
            InitZoneOpcode = 0x0,
            UnknownObfuscationInitOpcode = 0x0,
            OpcodeKeyTableOffset = opcodeKeyTableOffset,
            OpcodeKeyTableSize = opcodeKeyTableSize,
            ObfuscatedOpcodes = new Dictionary<string, int>
            {
                { "PlayerSpawn", opcodes["PlayerSpawn"] },
                { "NpcSpawn", opcodes["NpcSpawn"] },
                { "NpcSpawn2", opcodes["NpcSpawn2"] },

                { "ActionEffect01", opcodes["Ability1"] },
                { "ActionEffect08", opcodes["Ability8"] },
                { "ActionEffect16", opcodes["Ability16"] },
                { "ActionEffect24", opcodes["Ability24"] },
                { "ActionEffect32", opcodes["Ability32"] },

                { "StatusEffectList", opcodes["StatusEffectList"] },
                { "StatusEffectList3", opcodes["StatusEffectList3"] },

                { "Examine", 0x0 },
                { "UpdateGearset", 0x0 },
                { "UpdateParty", 0x0 },
                { "ActorControl", opcodes["ActorControl"] },
                { "ActorCast", opcodes["ActorCast"] },

                { "UnknownEffect01", 0x0 },
                { "UnknownEffect16", 0x0 },
                { "ActionEffect02", 0x0 },
                { "ActionEffect04", 0x0 }
            }
        };
    }
}
