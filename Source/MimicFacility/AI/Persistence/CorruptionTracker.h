// CorruptionTracker.h — Persistent corruption index tracking how players treat The Director across sessions.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "CorruptionTracker.generated.h"

UENUM(BlueprintType)
enum class ECorruptionPhase : uint8
{
	Cain            UMETA(DisplayName = "Cain (0-25)"),
	Transition      UMETA(DisplayName = "Transition (26-50)"),
	AMEmerging      UMETA(DisplayName = "AM Emerging (51-75)"),
	FullAM          UMETA(DisplayName = "Full AM (76-100)")
};

UENUM(BlueprintType)
enum class ECorruptionEvent : uint8
{
	// Increments
	PlayerMockedDirector,           // +5
	SessionCompletedNoEngagement,   // +3
	PlayerLiedToDirector,           // +3
	PlayerTreatedAsMechanic,        // +2
	PlayerDisconnected,             // +5
	PlayerSkippedDialogue,          // +2

	// Decrements
	PlayerAnsweredSincerely,        // -2
	PlayerThankedDirector,          // -1
	PlayerReferencedBetweenSessions,// -3
	PlayerStayedForCredits,         // -1
	PlayerApologized                // -5
};

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FOnCorruptionChanged, int32, NewValue, ECorruptionPhase, NewPhase);

UCLASS(BlueprintType)
class MIMICFACILITY_API UCorruptionTracker : public UObject
{
	GENERATED_BODY()

public:
	UCorruptionTracker();

	UFUNCTION(BlueprintCallable, Category = "Corruption")
	void ProcessEvent(ECorruptionEvent Event);

	UFUNCTION(BlueprintPure, Category = "Corruption")
	int32 GetCorruptionIndex() const { return CorruptionIndex; }

	UFUNCTION(BlueprintPure, Category = "Corruption")
	ECorruptionPhase GetCurrentPhase() const;

	UFUNCTION(BlueprintCallable, Category = "Corruption")
	void SetCorruptionIndex(int32 Value);

	UPROPERTY(BlueprintAssignable, Category = "Corruption")
	FOnCorruptionChanged OnCorruptionChanged;

private:
	void ApplyDelta(int32 Delta);
	int32 GetEventDelta(ECorruptionEvent Event) const;

	UPROPERTY()
	int32 CorruptionIndex;
};
