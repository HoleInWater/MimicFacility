// ContainmentDevice.h — Single-use trap for capturing identified mimics. Wrong target wastes the device.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GearBase.h"
#include "ContainmentDevice.generated.h"

UCLASS()
class MIMICFACILITY_API AContainmentDevice : public AGearBase
{
	GENERATED_BODY()

public:
	AContainmentDevice();

	virtual void Activate() override;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Gear|Containment")
	float ContainmentRadius;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Gear|Containment")
	float StunDuration;
};
