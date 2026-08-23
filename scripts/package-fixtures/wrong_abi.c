// SPDX-License-Identifier: MIT

#include <stdint.h>

#if defined(_WIN32)
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif

EXPORT uint32_t cna_get_abi_version(void) { return UINT32_C(1) << 16; }
EXPORT uint32_t cna_error_get_last_message_size(uint64_t* out_bytes)
{
    if (out_bytes != 0) { *out_bytes = 0; }
    return 0;
}
EXPORT uint32_t cna_game_create(const void* create_info, uint64_t* out_game)
{
    (void)create_info;
    if (out_game != 0) { *out_game = 0; }
    return 12;
}
EXPORT uint32_t cna_game_destroy(uint64_t game)
{
    (void)game;
    return 0;
}
