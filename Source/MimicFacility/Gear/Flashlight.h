// Flashlight.h — Flashlight gear item. Provides illumination and subtle Mimic shadow detection.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GearBase.h"
#include "Flashlight.generated.h"

class USpotLightComponent;

/**
 * AFlashlight
 * Player-held flashlight. When active, casts a spotlight.
 * Mimics cast subtly incorrect shadows under direct flashlight,
 * providing an attentive player with a detection method.
 */
UCLASS()
class MIMICFACILITY_API AFlashlight : public AGearBase
{
	GENERATED_BODY()

public:
	AFlashlight();

	virtual void Activate() override;

protected:
	/** Spotlight component for the flashlight beam. */
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Gear|Flashlight")
	TObjectPtr<USpotLightComponent> SpotLight;

	/** Whether the flashlight is currently on. */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Gear|Flashlight")
	bool bIsOn;
};
