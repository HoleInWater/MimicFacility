// WindowFocusObserver.cpp — Plays mid-sentence Director whispers when player returns from alt-tab.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "WindowFocusObserver.h"
#include "Sound/SoundBase.h"
#include "Kismet/GameplayStatics.h"
#include "Misc/CoreDelegates.h"

UWindowFocusObserver::UWindowFocusObserver()
{
	bIsInBackground = false;
	TimeEnteredBackground = 0.0;
	MinAltTabDuration = 3.0f;

	WhisperPool.Add(TSoftObjectPtr<USoundBase>(FSoftObjectPath(TEXT("/Game/Audio/Horror/Whisper_MidSentence_01"))));
	WhisperPool.Add(TSoftObjectPtr<USoundBase>(FSoftObjectPath(TEXT("/Game/Audio/Horror/Whisper_MidSentence_02"))));
	WhisperPool.Add(TSoftObjectPtr<USoundBase>(FSoftObjectPath(TEXT("/Game/Audio/Horror/Whisper_MidSentence_03"))));
	WhisperPool.Add(TSoftObjectPtr<USoundBase>(FSoftObjectPath(TEXT("/Game/Audio/Horror/Whisper_MidSentence_04"))));
}

void UWindowFocusObserver::Initialize()
{
	ForegroundHandle = FCoreDelegates::ApplicationHasEnteredForegroundDelegate.AddUObject(this, &UWindowFocusObserver::OnApplicationActivated);
	BackgroundHandle = FCoreDelegates::ApplicationWillEnterBackgroundDelegate.AddUObject(this, &UWindowFocusObserver::OnApplicationDeactivated);
}

void UWindowFocusObserver::Shutdown()
{
	FCoreDelegates::ApplicationHasEnteredForegroundDelegate.Remove(ForegroundHandle);
	FCoreDelegates::ApplicationWillEnterBackgroundDelegate.Remove(BackgroundHandle);
}

void UWindowFocusObserver::OnApplicationDeactivated()
{
	bIsInBackground = true;
	TimeEnteredBackground = FPlatformTime::Seconds();
}

void UWindowFocusObserver::OnApplicationActivated()
{
	if (!bIsInBackground) return;
	bIsInBackground = false;

	double TimeAway = FPlatformTime::Seconds() - TimeEnteredBackground;
	if (TimeAway >= MinAltTabDuration)
	{
		PlayWhisperClip();
	}
}

void UWindowFocusObserver::PlayWhisperClip()
{
	if (WhisperPool.Num() == 0) return;

	int32 Index = FMath::RandRange(0, WhisperPool.Num() - 1);
	USoundBase* Sound = WhisperPool[Index].LoadSynchronous();
	if (!Sound) return;

	UWorld* World = GetWorld();
	if (!World) return;

	UGameplayStatics::SpawnSound2D(World, Sound, 0.6f, 1.0f, 0.0f, nullptr, false, false);
}
