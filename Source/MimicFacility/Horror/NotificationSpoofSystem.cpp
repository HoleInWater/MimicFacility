// NotificationSpoofSystem.cpp — OS-detecting notification spoof for meta-horror.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "NotificationSpoofSystem.h"
#include "Sound/SoundBase.h"
#include "Components/AudioComponent.h"
#include "Kismet/GameplayStatics.h"

UNotificationSpoofSystem::UNotificationSpoofSystem()
{
	WindowsNotificationSound = TSoftObjectPtr<USoundBase>(FSoftObjectPath(TEXT("/Game/Audio/Horror/SFX_NotificationWindows")));
	MacNotificationSound = TSoftObjectPtr<USoundBase>(FSoftObjectPath(TEXT("/Game/Audio/Horror/SFX_NotificationMac")));
	LinuxNotificationSound = TSoftObjectPtr<USoundBase>(FSoftObjectPath(TEXT("/Game/Audio/Horror/SFX_NotificationLinux")));
}

void UNotificationSpoofSystem::PlayNotificationSound()
{
	USoundBase* Sound = ResolveNotificationSound();
	if (!Sound) return;

	UWorld* World = GetWorld();
	if (!World) return;

	UAudioComponent* AudioComp = UGameplayStatics::SpawnSound2D(World, Sound, 1.0f, 1.0f, 0.0f, nullptr, false, false);
	if (AudioComp)
	{
		AudioComp->bIsUISound = true;
	}
}

USoundBase* UNotificationSpoofSystem::ResolveNotificationSound() const
{
#if PLATFORM_WINDOWS
	return WindowsNotificationSound.LoadSynchronous();
#elif PLATFORM_MAC
	return MacNotificationSound.LoadSynchronous();
#elif PLATFORM_LINUX
	return LinuxNotificationSound.LoadSynchronous();
#else
	return WindowsNotificationSound.LoadSynchronous();
#endif
}
