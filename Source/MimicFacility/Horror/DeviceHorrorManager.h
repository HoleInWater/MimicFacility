// DeviceHorrorManager.h — Meta-horror system that orchestrates fourth-wall-breaking device tricks.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "DeviceHorrorManager.generated.h"

class UNotificationSpoofSystem;
class UWindowFocusObserver;
class USubliminalFrameSystem;
class UPauseMenuInjection;
class ULoadingScreenSystem;
class UCorruptionTracker;

UENUM(BlueprintType)
enum class EDeviceTrick : uint8
{
	SubliminalFrame,
	NotificationSpoof,
	PauseMenuInject,
	WindowFocusWhisper,
	LoadingScreenEntry,
	ScreenFreeze
};

UCLASS(ClassGroup = (Horror), meta = (BlueprintSpawnableComponent))
class MIMICFACILITY_API UDeviceHorrorManager : public UActorComponent
{
	GENERATED_BODY()

public:
	UDeviceHorrorManager();

	virtual void BeginPlay() override;
	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

	UFUNCTION(BlueprintCallable, Category = "Horror")
	void ScheduleTrick(EDeviceTrick Trick, float Delay);

	UFUNCTION(BlueprintCallable, Category = "Horror")
	void ExecuteTrick(EDeviceTrick Trick);

	UFUNCTION(BlueprintPure, Category = "Horror")
	bool HasTrickBeenUsed(EDeviceTrick Trick) const;

	UFUNCTION(BlueprintPure, Category = "Horror")
	static int32 MinCorruptionForTrick(EDeviceTrick Trick);

	UPROPERTY()
	TObjectPtr<UNotificationSpoofSystem> NotificationSpoof;

	UPROPERTY()
	TObjectPtr<UWindowFocusObserver> WindowFocusObserver;

	UPROPERTY()
	TObjectPtr<USubliminalFrameSystem> SubliminalFrame;

	UPROPERTY()
	TObjectPtr<UPauseMenuInjection> PauseMenuInjection;

	UPROPERTY()
	TObjectPtr<ULoadingScreenSystem> LoadingScreen;

private:
	void MarkTrickUsed(EDeviceTrick Trick);
	bool CanExecuteTrick(EDeviceTrick Trick) const;

	UPROPERTY()
	TSet<uint8> UsedTricks;

	UPROPERTY()
	TWeakObjectPtr<UCorruptionTracker> CachedCorruptionTracker;

	float LastTrickTime;

	UPROPERTY(EditDefaultsOnly, Category = "Horror")
	float TrickCooldown;

	struct FPendingTrick
	{
		EDeviceTrick Trick;
		float RemainingDelay;
	};
	TArray<FPendingTrick> PendingTricks;
};
