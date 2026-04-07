// WindowFocusObserver.h — Tracks alt-tab behavior and plays Director whispers on return.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "WindowFocusObserver.generated.h"

class USoundBase;

UCLASS(BlueprintType)
class MIMICFACILITY_API UWindowFocusObserver : public UObject
{
	GENERATED_BODY()

public:
	UWindowFocusObserver();

	void Initialize();
	void Shutdown();

private:
	void OnApplicationActivated();
	void OnApplicationDeactivated();

	void PlayWhisperClip();

	UPROPERTY()
	TArray<TSoftObjectPtr<USoundBase>> WhisperPool;

	double TimeEnteredBackground;
	bool bIsInBackground;

	UPROPERTY()
	float MinAltTabDuration;

	FDelegateHandle ForegroundHandle;
	FDelegateHandle BackgroundHandle;
};
