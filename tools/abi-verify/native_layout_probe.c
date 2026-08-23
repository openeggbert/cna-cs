// SPDX-License-Identifier: MIT

#include <stddef.h>
#include <stdint.h>
#include <stdio.h>

#include "CNA/C/cna.h"

#define PRINT_SIZE(type) printf("sizeof." #type "=%zu\n", sizeof(type))
#define PRINT_ALIGN(type) printf("alignof." #type "=%zu\n", _Alignof(type))
#define PRINT_OFFSET(type, field) printf("offsetof." #type "." #field "=%zu\n", offsetof(type, field))

int main(void)
{
    printf("abi.version=%u\n", CNA_ABI_VERSION);
    PRINT_SIZE(void*);
    PRINT_ALIGN(void*);
    PRINT_SIZE(CNA_Result);
    PRINT_ALIGN(CNA_Result);
    PRINT_SIZE(CNA_Bool);
    PRINT_ALIGN(CNA_Bool);
    PRINT_SIZE(CNA_Handle);
    PRINT_ALIGN(CNA_Handle);
    PRINT_SIZE(CNA_GraphicsDeviceEvent);
    PRINT_SIZE(CNA_GraphicsProfile);

    PRINT_SIZE(CNA_StringView);
    PRINT_ALIGN(CNA_StringView);
    PRINT_OFFSET(CNA_StringView, data);
    PRINT_OFFSET(CNA_StringView, byte_length);

    PRINT_SIZE(CNA_SoundEffectCreateInfo);
    PRINT_ALIGN(CNA_SoundEffectCreateInfo);
    PRINT_OFFSET(CNA_SoundEffectCreateInfo, struct_size);
    PRINT_OFFSET(CNA_SoundEffectCreateInfo, struct_version);
    PRINT_OFFSET(CNA_SoundEffectCreateInfo, sample_rate);
    PRINT_OFFSET(CNA_SoundEffectCreateInfo, channels);
    PRINT_OFFSET(CNA_SoundEffectCreateInfo, reserved);

    PRINT_SIZE(CNA_SoundEffectInstanceInfo);
    PRINT_ALIGN(CNA_SoundEffectInstanceInfo);
    PRINT_OFFSET(CNA_SoundEffectInstanceInfo, struct_size);
    PRINT_OFFSET(CNA_SoundEffectInstanceInfo, struct_version);
    PRINT_OFFSET(CNA_SoundEffectInstanceInfo, state);
    PRINT_OFFSET(CNA_SoundEffectInstanceInfo, is_looped);
    PRINT_OFFSET(CNA_SoundEffectInstanceInfo, reserved0);
    PRINT_OFFSET(CNA_SoundEffectInstanceInfo, volume);
    PRINT_OFFSET(CNA_SoundEffectInstanceInfo, pitch);
    PRINT_OFFSET(CNA_SoundEffectInstanceInfo, pan);
    PRINT_OFFSET(CNA_SoundEffectInstanceInfo, reserved1);

    PRINT_SIZE(CNA_AudioListener);
    PRINT_ALIGN(CNA_AudioListener);
    PRINT_OFFSET(CNA_AudioListener, struct_size);
    PRINT_OFFSET(CNA_AudioListener, struct_version);
    PRINT_OFFSET(CNA_AudioListener, forward);
    PRINT_OFFSET(CNA_AudioListener, position);
    PRINT_OFFSET(CNA_AudioListener, up);
    PRINT_OFFSET(CNA_AudioListener, velocity);

    PRINT_SIZE(CNA_AudioEmitter);
    PRINT_ALIGN(CNA_AudioEmitter);
    PRINT_OFFSET(CNA_AudioEmitter, struct_size);
    PRINT_OFFSET(CNA_AudioEmitter, struct_version);
    PRINT_OFFSET(CNA_AudioEmitter, doppler_scale);
    PRINT_OFFSET(CNA_AudioEmitter, forward);
    PRINT_OFFSET(CNA_AudioEmitter, position);
    PRINT_OFFSET(CNA_AudioEmitter, up);
    PRINT_OFFSET(CNA_AudioEmitter, velocity);

    PRINT_SIZE(CNA_CueInfo);
    PRINT_ALIGN(CNA_CueInfo);
    PRINT_OFFSET(CNA_CueInfo, struct_size);
    PRINT_OFFSET(CNA_CueInfo, struct_version);
    PRINT_OFFSET(CNA_CueInfo, is_created);
    PRINT_OFFSET(CNA_CueInfo, is_disposed);
    PRINT_OFFSET(CNA_CueInfo, is_paused);
    PRINT_OFFSET(CNA_CueInfo, is_playing);
    PRINT_OFFSET(CNA_CueInfo, is_prepared);
    PRINT_OFFSET(CNA_CueInfo, is_preparing);
    PRINT_OFFSET(CNA_CueInfo, is_stopped);
    PRINT_OFFSET(CNA_CueInfo, is_stopping);

    PRINT_SIZE(CNA_VisualizationData);
    PRINT_ALIGN(CNA_VisualizationData);
    PRINT_OFFSET(CNA_VisualizationData, struct_size);
    PRINT_OFFSET(CNA_VisualizationData, struct_version);
    PRINT_OFFSET(CNA_VisualizationData, frequencies);
    PRINT_OFFSET(CNA_VisualizationData, samples);

    PRINT_SIZE(CNA_GameCallbacks);
    PRINT_ALIGN(CNA_GameCallbacks);
    PRINT_OFFSET(CNA_GameCallbacks, struct_size);
    PRINT_OFFSET(CNA_GameCallbacks, struct_version);
    PRINT_OFFSET(CNA_GameCallbacks, load_content);
    PRINT_OFFSET(CNA_GameCallbacks, update);
    PRINT_OFFSET(CNA_GameCallbacks, draw);
    PRINT_OFFSET(CNA_GameCallbacks, unload_content);
    PRINT_OFFSET(CNA_GameCallbacks, exiting);
    PRINT_OFFSET(CNA_GameCallbacks, context);

    PRINT_SIZE(CNA_GameCreateInfo);
    PRINT_ALIGN(CNA_GameCreateInfo);
    PRINT_OFFSET(CNA_GameCreateInfo, struct_size);
    PRINT_OFFSET(CNA_GameCreateInfo, struct_version);
    PRINT_OFFSET(CNA_GameCreateInfo, is_fixed_time_step);
    PRINT_OFFSET(CNA_GameCreateInfo, reserved);
    PRINT_OFFSET(CNA_GameCreateInfo, target_elapsed_time_ticks);
    PRINT_OFFSET(CNA_GameCreateInfo, window_title);
    PRINT_OFFSET(CNA_GameCreateInfo, callbacks);

    return 0;
}
