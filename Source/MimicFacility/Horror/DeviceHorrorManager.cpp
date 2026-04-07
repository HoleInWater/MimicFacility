// DeviceHorrorManager.cpp — Orchestrates meta-horror device tricks with corruption gating.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "DeviceHorrorManager.h"
#include "NotificationSpoofSystem.h"
#include "WindowFocusObserver.h"
#include "SubliminalFrameSystem.h"
#include "PauseMenuInjection.h"
#include "LoadingScreenSystem.h"
#include "AI/DirectorAI.h"
#include "AI/Persistence/CorruptionTracker.h"

UDeviceHorrorManager::UDeviceHorrorManager()
{
	PrimaryComponentTick.bCanEverTick = true;
	TrickCooldown = 120.0f;
	LastTrickTime = -TrickCooldown;
}

void UDeviceHorrorManager::BeginPlay()
{
	Super::BeginPlay();

	NotificationSpoof = NewObject<UNotificationSpoofSystem>(this);
	WindowFocusObserver = NewObject<UWindowFocusObserver>(this);
	SubliminalFrame = NewObject<USubliminalFrameSystem>(this);
	PauseMenuInjection = NewObject<UPauseMenuInjection>(this);
	LoadingScreen = NewObject<ULoadingScreenSystem>(this);

	WindowFocusObserver->Initialize();

	if (ADirectorAI* Director = Cast<ADirectorAI>(GetOwner()))
	{
		CachedCorruptionTracker = Director->GetCorruptionTracker();
	}
}

void UDeviceHorrorManager::TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	for (int32 i = PendingTricks.Num() - 1; i >= 0; --i)
	{
		PendingTricks[i].RemainingDelay -= DeltaTime;
		if (PendingTricks[i].RemainingDelay <= 0.0f)
		{
			ExecuteTrick(PendingTricks[i].Trick);
			PendingTricks.RemoveAt(i);
		}
	}
}

void UDeviceHorrorManager::ScheduleTrick(EDeviceTrick Trick, float Delay)
{
	if (!CanExecuteTrick(Trick)) return;
	PendingTricks.Add({ Trick, Delay });
}

void UDeviceHorrorManager::ExecuteTrick(EDeviceTrick Trick)
{
	if (!CanExecuteTrick(Trick)) return;

	switch (Trick)
	{
	case EDeviceTrick::SubliminalFrame:
		SubliminalFrame->RenderSubliminalFrame();
		break;
	case EDeviceTrick::NotificationSpoof:
		NotificationSpoof->PlayNotificationSound();
		break;
	case EDeviceTrick::PauseMenuInject:
		PauseMenuInjection->InjectIntoPauseMenu();
		break;
	case EDeviceTrick::WindowFocusWhisper:
		// Handled reactively by WindowFocusObserver delegates
		break;
	case EDeviceTrick::LoadingScreenEntry:
		LoadingScreen->BeginLoadingScreen();
		break;
	case EDeviceTrick::ScreenFreeze:
		// Freeze handled externally via post-process or input blocking
		break;
	}

	MarkTrickUsed(Trick);
	LastTrickTime = GetWorld()->GetTimeSeconds();
}

bool UDeviceHorrorManager::HasTrickBeenUsed(EDeviceTrick Trick) const
{
	return UsedTricks.Contains(static_cast<uint8>(Trick));
}

void UDeviceHorrorManager::MarkTrickUsed(EDeviceTrick Trick)
{
	UsedTricks.Add(static_cast<uint8>(Trick));
}

bool UDeviceHorrorManager::CanExecuteTrick(EDeviceTrick Trick) const
{
	if (HasTrickBeenUsed(Trick)) return false;

	float CurrentTime = GetWorld()->GetTimeSeconds();
	if (CurrentTime - LastTrickTime < TrickCooldown) return false;

	if (CachedCorruptionTracker.IsValid())
	{
		int32 Corruption = CachedCorruptionTracker->GetCorruptionIndex();
		if (Corruption < MinCorruptionForTrick(Trick)) return false;
	}

	return true;
}

int32 UDeviceHorrorManager::MinCorruptionForTrick(EDeviceTrick Trick)
{
	switch (Trick)
	{
	case EDeviceTrick::LoadingScreenEntry:  return 10;
	case EDeviceTrick::SubliminalFrame:     return 15;
	case EDeviceTrick::NotificationSpoof:   return 25;
	case EDeviceTrick::PauseMenuInject:     return 40;
	case EDeviceTrick::WindowFocusWhisper:  return 50;
	case EDeviceTrick::ScreenFreeze:        return 60;
	default:                                return 100;
	}
}
