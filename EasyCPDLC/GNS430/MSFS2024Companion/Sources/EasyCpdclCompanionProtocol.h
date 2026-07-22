#pragma once

#include <cstdint>

namespace easycpdlc
{
    constexpr std::uint32_t kMagic = 0x45435044;
    constexpr std::uint32_t kVersion = 1;
    constexpr std::uint32_t kChecksumSeed = 0x430C0DEC;
    constexpr const char* kCommandClientDataName = "EasyCPDLC.GNS430.Command.v1";
    constexpr const char* kStatusClientDataName = "EasyCPDLC.GNS430.Status.v1";

    constexpr const char* kCommandLVar = "EASYCPDLC_GNS_COMMAND";
    constexpr const char* kModuleAliveLVar = "EASYCPDLC_GNS_MODULE_ALIVE";
    constexpr const char* kAppConnectedLVar = "EASYCPDLC_GNS_APP_CONNECTED";
    constexpr const char* kVatsimConnectedLVar = "EASYCPDLC_GNS_VATSIM_CONNECTED";
    constexpr const char* kUnreadCountLVar = "EASYCPDLC_GNS_UNREAD_COUNT";
    constexpr const char* kPageLVar = "EASYCPDLC_GNS_PAGE";
    constexpr const char* kCursorActiveLVar = "EASYCPDLC_GNS_CURSOR_ACTIVE";

    constexpr std::uint32_t kStatusAppOnline = 1u << 0;
    constexpr std::uint32_t kStatusVatsimConnected = 1u << 1;
    constexpr std::uint32_t kStatusCursorActive = 1u << 2;

#pragma pack(push, 1)
    struct CommandPacket
    {
        std::uint32_t magic;
        std::uint32_t version;
        std::uint32_t sequence;
        std::uint32_t command;
        std::uint32_t checksum;
    };

    struct StatusPacket
    {
        std::uint32_t magic;
        std::uint32_t version;
        std::uint32_t sequence;
        std::uint32_t flags;
        std::uint32_t unreadCount;
        std::uint32_t page;
        std::uint32_t reserved0;
        std::uint32_t reserved1;
    };
#pragma pack(pop)

    static_assert(sizeof(CommandPacket) == 20, "Command protocol layout changed");
    static_assert(sizeof(StatusPacket) == 32, "Status protocol layout changed");

    constexpr std::uint32_t CommandChecksum(std::uint32_t sequence, std::uint32_t command)
    {
        return kChecksumSeed ^ kMagic ^ kVersion ^ sequence ^ command;
    }
}
