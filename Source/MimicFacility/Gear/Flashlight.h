// Flashlight.h — Flashlight gear item. Provides illumination and subtle Mimic shadow detection.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GearBase.h"
#include "Flashlight.generated.h"

class USpotLightComponent;

UCLASS()
class MIMICFACILITY_API AFlashlight : public AGearBase
{
	GENERATED_BODY()

public:
	AFlashlight();

	virtual void Activate() override;

protected:
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Gear|Flashlight")
	TObjectPtr<USpotLightComponent> SpotLight;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Gear|Flashlight")
	bool bIsOn;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
