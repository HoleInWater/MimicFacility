// GearBase.h — Base class for all player gear items. Handles pickup, equip, and use interface.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "GearBase.generated.h"

/**
 * AGearBase
 * Abstract base class for all gear items (flashlight, scanner, containment device, etc.).
 * Provides shared pickup/drop/equip logic and defines the interface for gear activation.
 */
UCLASS(Abstract)
class MIMICFACILITY_API AGearBase : public AActor
{
	GENERATED_BODY()

public:
	AGearBase();

protected:
	virtual void BeginPlay() override;

public:
	/** Called when the player activates this gear item. */
	UFUNCTION(BlueprintCallable, Category = "Gear")
	virtual void Activate();

	/** Called when the player picks up this gear item. */
	UFUNCTION(BlueprintCallable, Category = "Gear")
	virtual void OnPickedUp(AActor* NewOwner);

protected:
	/** Display name shown in the HUD. */
	UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = "Gear")
	FText GearName;

	/** Whether this gear item has limited uses. */
	UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = "Gear")
	bool bIsConsumable;

	/** Remaining uses (if consumable). */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Gear")
	int32 UsesRemaining;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
