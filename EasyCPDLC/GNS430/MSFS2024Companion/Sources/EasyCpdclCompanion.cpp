#include "EasyCpdclCompanionProtocol.h"

#include <MSFS/MSFS.h>
#if defined(__wasm__)
typedef long long __int64;
#endif
#include <MSFS/MSFS_WindowsTypes.h>
#include <SimConnect.h>

#include <cstdio>
#include <cstddef>
#include <cstring>

extern "C"
{
    // This legacy ABI is available to standalone modules in both simulator
    // generations. The newer fsVars imports are not exposed by every MSFS 2024
    // standalone-module host.
    std::int32_t register_named_variable(const char* name);
    double get_named_variable_value(std::int32_t id);
    void set_named_variable_value(std::int32_t id, double value);
}

namespace
{
    enum ClientDataId : DWORD
    {
        CommandClientDataId = 1,
        StatusClientDataId = 2
    };

    enum DefinitionId : DWORD
    {
        CommandDefinitionId = 1,
        StatusDefinitionId = 2
    };

    enum RequestId : DWORD
    {
        StatusRequestId = 1
    };

    enum EventId : DWORD
    {
        OneSecondEventId = 100
    };

    HANDLE g_simConnect = 0;
    std::uint32_t g_commandSequence = 0;
    float g_heartbeatSeconds = 0.0f;
    float g_statusAgeSeconds = 1000.0f;
    using LVarId = std::int32_t;
    constexpr LVarId InvalidLVarId = -1;
    LVarId g_command = InvalidLVarId;
    LVarId g_moduleAlive = InvalidLVarId;
    LVarId g_appConnected = InvalidLVarId;
    LVarId g_vatsimConnected = InvalidLVarId;
    LVarId g_unreadCount = InvalidLVarId;
    LVarId g_page = InvalidLVarId;
    LVarId g_cursorActive = InvalidLVarId;
    LVarId g_dcduModeLVar = InvalidLVarId;
    bool g_dcduMode = false;

    struct DcduInput
    {
        const char* name;
        std::uint32_t command;
        LVarId id;
    };

    DcduInput g_dcduInputs[] =
    {
        { "EASYCPDLC_DCDU_LSK_L1", 19, InvalidLVarId },
        { "EASYCPDLC_DCDU_LSK_L2", 20, InvalidLVarId },
        { "EASYCPDLC_DCDU_LSK_L3", 21, InvalidLVarId },
        { "EASYCPDLC_DCDU_LSK_L4", 22, InvalidLVarId },
        { "EASYCPDLC_DCDU_LSK_L5", 23, InvalidLVarId },
        { "EASYCPDLC_DCDU_LSK_L6", 24, InvalidLVarId },
        { "EASYCPDLC_DCDU_LSK_R1", 25, InvalidLVarId },
        { "EASYCPDLC_DCDU_LSK_R2", 26, InvalidLVarId },
        { "EASYCPDLC_DCDU_LSK_R3", 27, InvalidLVarId },
        { "EASYCPDLC_DCDU_LSK_R4", 28, InvalidLVarId },
        { "EASYCPDLC_DCDU_LSK_R5", 29, InvalidLVarId },
        { "EASYCPDLC_DCDU_LSK_R6", 30, InvalidLVarId },
        { "EASYCPDLC_DCDU_CONNECT", 31, InvalidLVarId },
        { "EASYCPDLC_DCDU_AOC", 32, InvalidLVarId },
        { "EASYCPDLC_DCDU_ATC", 33, InvalidLVarId },
        { "EASYCPDLC_DCDU_SETTINGS", 34, InvalidLVarId },
        { "EASYCPDLC_DCDU_RELOAD", 35, InvalidLVarId },
        { "EASYCPDLC_DCDU_PRINT", 36, InvalidLVarId },
        { "EASYCPDLC_DCDU_REPRINT", 37, InvalidLVarId },
        { "EASYCPDLC_DCDU_HIDE", 38, InvalidLVarId }
    };

    void SetLVar(LVarId id, double value)
    {
        if (id != InvalidLVarId)
        {
            set_named_variable_value(id, value);
        }
    }

    void PublishCommand(std::uint32_t command)
    {
        if (g_simConnect == 0)
        {
            return;
        }

        easycpdlc::CommandPacket packet{};
        packet.magic = easycpdlc::kMagic;
        packet.version = easycpdlc::kVersion;
        packet.sequence = ++g_commandSequence;
        packet.command = command;
        packet.checksum = easycpdlc::CommandChecksum(packet.sequence, packet.command);

        SimConnect_SetClientData(
            g_simConnect,
            CommandClientDataId,
            CommandDefinitionId,
            SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT,
            0,
            sizeof(packet),
            &packet);
    }

    void ClearDcduInputs()
    {
        for (auto& input : g_dcduInputs)
        {
            SetLVar(input.id, 0.0);
        }
    }

    void ApplyStatus(const easycpdlc::StatusPacket& packet)
    {
        if (packet.magic != easycpdlc::kMagic || packet.version != easycpdlc::kVersion)
        {
            return;
        }

        g_statusAgeSeconds = 0.0f;
        SetLVar(g_appConnected, (packet.flags & easycpdlc::kStatusAppOnline) != 0 ? 1.0 : 0.0);
        SetLVar(g_vatsimConnected, (packet.flags & easycpdlc::kStatusVatsimConnected) != 0 ? 1.0 : 0.0);
        SetLVar(g_cursorActive, (packet.flags & easycpdlc::kStatusCursorActive) != 0 ? 1.0 : 0.0);
        const bool dcduMode = (packet.flags & easycpdlc::kStatusDcduMode) != 0;
        if (g_dcduMode && !dcduMode)
        {
            ClearDcduInputs();
        }
        g_dcduMode = dcduMode;
        SetLVar(g_dcduModeLVar, g_dcduMode ? 1.0 : 0.0);
        SetLVar(g_unreadCount, static_cast<double>(packet.unreadCount));
        SetLVar(g_page, static_cast<double>(packet.page));
    }

    void ProcessTick(float elapsed);

    void CALLBACK Dispatch(SIMCONNECT_RECV* data, DWORD size, void*)
    {
        if (data == nullptr)
        {
            return;
        }

        if (data->dwID == SIMCONNECT_RECV_ID_CLIENT_DATA)
        {
            const auto* received = reinterpret_cast<const SIMCONNECT_RECV_CLIENT_DATA*>(data);
            constexpr std::size_t payloadOffset = offsetof(SIMCONNECT_RECV_CLIENT_DATA, dwData);
            if (received->dwRequestID != StatusRequestId ||
                size < payloadOffset + sizeof(easycpdlc::StatusPacket))
            {
                return;
            }

            easycpdlc::StatusPacket packet{};
            std::memcpy(&packet, &received->dwData, sizeof(packet));
            ApplyStatus(packet);
            return;
        }

        if (data->dwID == SIMCONNECT_RECV_ID_EVENT)
        {
            const auto* eventData = reinterpret_cast<const SIMCONNECT_RECV_EVENT*>(data);
            if (eventData->uEventID == OneSecondEventId)
            {
                ProcessTick(1.0f);
            }
        }
    }

    bool OpenClientDataChannels()
    {
        if (SimConnect_Open(&g_simConnect, "EasyCPDLC MSFS 2024 companion", 0, 0, 0, 0) < 0 ||
            g_simConnect == 0)
        {
            g_simConnect = 0;
            return false;
        }

        if (SimConnect_MapClientDataNameToID(g_simConnect, easycpdlc::kCommandClientDataName, CommandClientDataId) < 0 ||
            SimConnect_MapClientDataNameToID(g_simConnect, easycpdlc::kStatusClientDataName, StatusClientDataId) < 0 ||
            SimConnect_AddToClientDataDefinition(g_simConnect, CommandDefinitionId, 0, sizeof(easycpdlc::CommandPacket)) < 0 ||
            SimConnect_AddToClientDataDefinition(g_simConnect, StatusDefinitionId, 0, sizeof(easycpdlc::StatusPacket)) < 0)
        {
            SimConnect_Close(g_simConnect);
            g_simConnect = 0;
            return false;
        }

        // Creation can report an error when the desktop endpoint started first and
        // already created the same named areas. The mapped channels remain usable.
        SimConnect_CreateClientData(g_simConnect, CommandClientDataId, sizeof(easycpdlc::CommandPacket), 0);
        SimConnect_CreateClientData(g_simConnect, StatusClientDataId, sizeof(easycpdlc::StatusPacket), 0);

        if (SimConnect_RequestClientData(
            g_simConnect,
            StatusClientDataId,
            StatusRequestId,
            StatusDefinitionId,
            SIMCONNECT_CLIENT_DATA_PERIOD_ON_SET,
            SIMCONNECT_CLIENT_DATA_REQUEST_FLAG_CHANGED) < 0)
        {
            SimConnect_Close(g_simConnect);
            g_simConnect = 0;
            return false;
        }

        if (SimConnect_SubscribeToSystemEvent(
                g_simConnect,
                OneSecondEventId,
                "1sec") < 0 ||
            SimConnect_CallDispatch(g_simConnect, Dispatch, nullptr) < 0)
        {
            SimConnect_Close(g_simConnect);
            g_simConnect = 0;
            return false;
        }

        return true;
    }

    void RegisterLVars()
    {
        g_command = register_named_variable(easycpdlc::kCommandLVar);
        g_moduleAlive = register_named_variable(easycpdlc::kModuleAliveLVar);
        g_appConnected = register_named_variable(easycpdlc::kAppConnectedLVar);
        g_vatsimConnected = register_named_variable(easycpdlc::kVatsimConnectedLVar);
        g_unreadCount = register_named_variable(easycpdlc::kUnreadCountLVar);
        g_page = register_named_variable(easycpdlc::kPageLVar);
        g_cursorActive = register_named_variable(easycpdlc::kCursorActiveLVar);
        g_dcduModeLVar = register_named_variable(easycpdlc::kDcduModeLVar);
        for (auto& input : g_dcduInputs)
        {
            input.id = register_named_variable(input.name);
        }

        SetLVar(g_command, 0.0);
        SetLVar(g_moduleAlive, 0.0);
        SetLVar(g_appConnected, 0.0);
        SetLVar(g_vatsimConnected, 0.0);
        SetLVar(g_unreadCount, 0.0);
        SetLVar(g_page, 0.0);
        SetLVar(g_cursorActive, 0.0);
        SetLVar(g_dcduModeLVar, 0.0);
        ClearDcduInputs();
    }

    void ProcessTick(float elapsed)
    {
        if (g_simConnect == 0)
        {
            return;
        }

        g_heartbeatSeconds += elapsed;
        g_statusAgeSeconds += elapsed;

        const double commandValue = get_named_variable_value(g_command);
        if (commandValue != 0.0)
        {
            SetLVar(g_command, 0.0);
            const auto command = static_cast<std::uint32_t>(commandValue + 0.5);
            if (!g_dcduMode && command >= 1 && command <= 18)
            {
                PublishCommand(command);
            }
        }

        if (g_dcduMode)
        {
            for (auto& input : g_dcduInputs)
            {
                const double value = get_named_variable_value(input.id);
                if (value != 0.0)
                {
                    SetLVar(input.id, 0.0);
                    PublishCommand(input.command);
                }
            }
        }
        else
        {
            ClearDcduInputs();
        }

        if (g_heartbeatSeconds >= 1.0f)
        {
            g_heartbeatSeconds = 0.0f;
            PublishCommand(0);
            SetLVar(g_moduleAlive, 1.0);
        }

        if (g_statusAgeSeconds >= 3.0f)
        {
            SetLVar(g_appConnected, 0.0);
            SetLVar(g_vatsimConnected, 0.0);
            SetLVar(g_dcduModeLVar, 0.0);
            g_dcduMode = false;
            ClearDcduInputs();
        }
    }
}

extern "C" MSFS_CALLBACK void module_init(void)
{
    RegisterLVars();
    const bool opened = OpenClientDataChannels();
    SetLVar(g_moduleAlive, opened ? 1.0 : 0.0);
    std::printf("[EasyCPDLC] companion module %s\n", opened ? "ready" : "could not open SimConnect");
}

extern "C" MSFS_CALLBACK void module_deinit(void)
{
    SetLVar(g_moduleAlive, 0.0);
    SetLVar(g_appConnected, 0.0);
    SetLVar(g_dcduModeLVar, 0.0);
    ClearDcduInputs();
    if (g_simConnect != 0)
    {
        SimConnect_Close(g_simConnect);
        g_simConnect = 0;
    }
    std::printf("[EasyCPDLC] companion module stopped\n");
}
