// MimicBase.h — Base class for all Mimic enemy types. Handles skin duplication, voice playback, and behavior tree binding.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "MimicBase.generated.h"

/**
 * EMimicState
 * Behavioral states for Mimic AI.
 */
UENUM(BlueprintType)
enum class EMimicState : uint8
{
	Infiltrating    UMETA(DisplayName = "Infiltrating"),
	Stalking        UMETA(DisplayName = "Stalking"),
	Aggressive      UMETA(DisplayName = "Aggressive"),
	Reproducing     UMETA(DisplayName = "Reproducing")
};

/**
 * AMimicBase
 * Base Mimic actor. All Mimic variants (Crawler, Swarm, etc.) inherit from this class.
 * Provides voice profile binding, skin replication, and shared Mimic logic.
 */
UCLASS()
class MIMICFACILITY_API AMimicBase : public ACharacter
{
	GENERATED_BODY()

public:
	AMimicBase();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;

protected:
	/** The ID of the player whose appearance and voice this Mimic copies. */
	UPROPERTY(ReplicatedUsing = OnRep_MimicSkin, BlueprintReadOnly, Category = "Mimic")
	FString VoiceProfileID;

	/** Current behavioral state of this Mimic. */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Mimic")
	EMimicState CurrentState;

	/** Whether a player has formally identified this Mimic. */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Mimic")
	bool bIsIdentified;

	UFUNCTION()
	void OnRep_MimicSkin();

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
