#include "EasyCpdclCompanionProtocol.h"

#include <MSFS/MSFS.h>
#include <MSFS/MSFS_Vars.h>
#include <SimConnect.h>

#include <cstdio>
#include <cstddef>
#include <cstring>

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

    HANDLE g_simConnect = nullptr;
    std::uint32_t g_commandSequence = 0;
    float g_heartbeatSeconds = 0.0f;
    float g_statusAgeSeconds = 1000.0f;
    FsUnitId g_numberUnit = -1;
    FsLVarId g_command = FS_VAR_INVALID_ID;
    FsLVarId g_moduleAlive = FS_VAR_INVALID_ID;
    FsLVarId g_appConnected = FS_VAR_INVALID_ID;
    FsLVarId g_vatsimConnected = FS_VAR_INVALID_ID;
    FsLVarId g_unreadCount = FS_VAR_INVALID_ID;
    FsLVarId g_page = FS_VAR_INVALID_ID;
    FsLVarId g_cursorActive = FS_VAR_INVALID_ID;

    void SetLVar(FsLVarId id, double value)
    {
        if (id != FS_VAR_INVALID_ID && g_numberUnit >= 0)
        {
            fsVarsLVarSet(id, g_numberUnit, value);
        }
    }

    void PublishCommand(std::uint32_t command)
    {
        if (g_simConnect == nullptr)
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
        SetLVar(g_unreadCount, static_cast<double>(packet.unreadCount));
        SetLVar(g_page, static_cast<double>(packet.page));
    }

    void CALLBACK Dispatch(SIMCONNECT_RECV* data, DWORD size, void*)
    {
        if (data == nullptr || data->dwID != SIMCONNECT_RECV_ID_CLIENT_DATA)
        {
            return;
        }

        const auto* received = reinterpret_cast<const SIMCONNECT_RECV_CLIENT_DATA*>(data);
        constexpr std::size_t payloadOffset = offsetof(SIMCONNECT_RECV_CLIENT_DATA, dwData);
        if (received->dwRequestID != StatusRequestId || size < payloadOffset + sizeof(easycpdlc::StatusPacket))
        {
            return;
        }

        easycpdlc::StatusPacket packet{};
        std::memcpy(&packet, &received->dwData, sizeof(packet));
        ApplyStatus(packet);
    }

    bool OpenClientDataChannels()
    {
        if (SimConnect_Open(&g_simConnect, "EasyCPDLC MSFS 2024 companion", nullptr, 0, nullptr, 0) < 0 ||
            g_simConnect == nullptr)
        {
            g_simConnect = nullptr;
            return false;
        }

        if (SimConnect_MapClientDataNameToID(g_simConnect, easycpdlc::kCommandClientDataName, CommandClientDataId) < 0 ||
            SimConnect_MapClientDataNameToID(g_simConnect, easycpdlc::kStatusClientDataName, StatusClientDataId) < 0 ||
            SimConnect_AddToClientDataDefinition(g_simConnect, CommandDefinitionId, 0, sizeof(easycpdlc::CommandPacket)) < 0 ||
            SimConnect_AddToClientDataDefinition(g_simConnect, StatusDefinitionId, 0, sizeof(easycpdlc::StatusPacket)) < 0)
        {
            SimConnect_Close(g_simConnect);
            g_simConnect = nullptr;
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
            g_simConnect = nullptr;
            return false;
        }

        return true;
    }

    void RegisterLVars()
    {
        g_numberUnit = fsVarsGetUnitId("Number");
        g_command = fsVarsRegisterLVar(easycpdlc::kCommandLVar);
        g_moduleAlive = fsVarsRegisterLVar(easycpdlc::kModuleAliveLVar);
        g_appConnected = fsVarsRegisterLVar(easycpdlc::kAppConnectedLVar);
        g_vatsimConnected = fsVarsRegisterLVar(easycpdlc::kVatsimConnectedLVar);
        g_unreadCount = fsVarsRegisterLVar(easycpdlc::kUnreadCountLVar);
        g_page = fsVarsRegisterLVar(easycpdlc::kPageLVar);
        g_cursorActive = fsVarsRegisterLVar(easycpdlc::kCursorActiveLVar);

        SetLVar(g_command, 0.0);
        SetLVar(g_moduleAlive, 0.0);
        SetLVar(g_appConnected, 0.0);
        SetLVar(g_vatsimConnected, 0.0);
        SetLVar(g_unreadCount, 0.0);
        SetLVar(g_page, 0.0);
        SetLVar(g_cursorActive, 0.0);
    }
}

extern "C" MSFS_CALLBACK void module_init(void)
{
    RegisterLVars();
    const bool opened = OpenClientDataChannels();
    SetLVar(g_moduleAlive, opened ? 1.0 : 0.0);
    std::printf("[EasyCPDLC] companion module %s\n", opened ? "ready" : "could not open SimConnect");
}

extern "C" MSFS_CALLBACK void module_update(float deltaSeconds)
{
    if (g_simConnect == nullptr)
    {
        return;
    }

    SimConnect_CallDispatch(g_simConnect, Dispatch, nullptr);
    const float elapsed = deltaSeconds > 0.0f ? deltaSeconds : 0.0f;
    g_heartbeatSeconds += elapsed;
    g_statusAgeSeconds += elapsed;

    double commandValue = 0.0;
    if (fsVarsLVarGet(g_command, g_numberUnit, &commandValue) == FS_VAR_ERROR_NONE && commandValue != 0.0)
    {
        SetLVar(g_command, 0.0);
        const auto command = static_cast<std::uint32_t>(commandValue + 0.5);
        if (command >= 1 && command <= 18)
        {
            PublishCommand(command);
        }
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
    }
}

extern "C" MSFS_CALLBACK void module_deinit(void)
{
    SetLVar(g_moduleAlive, 0.0);
    SetLVar(g_appConnected, 0.0);
    if (g_simConnect != nullptr)
    {
        SimConnect_Close(g_simConnect);
        g_simConnect = nullptr;
    }
    std::printf("[EasyCPDLC] companion module stopped\n");
}
