// SporeFilter.h — Wearable mask that reduces spore cloud hallucination effects (visual/audio distortion).
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GearBase.h"
#include "SporeFilter.generated.h"

UCLASS()
class MIMICFACILITY_API ASporeFilter : public AGearBase
{
	GENERATED_BODY()

public:
	ASporeFilter();

	virtual void Activate() override;

	UFUNCTION(BlueprintPure, Category = "Gear|Filter")
	bool IsWorn() const { return bIsWorn; }

	UFUNCTION(BlueprintPure, Category = "Gear|Filter")
	float GetFilterEfficiency() const { return FilterEfficiency; }

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Gear|Filter")
	float FilterEfficiency;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Gear|Filter")
	float FilterDuration;

private:
	bool bIsWorn;
	float WearTime;
};
