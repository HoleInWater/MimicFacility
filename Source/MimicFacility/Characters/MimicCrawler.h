// MimicCrawler.h — Ceiling Crawler Mimic variant. Uses ceiling-mounted pathfinding and ambush behavior.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "MimicBase.h"
#include "MimicCrawler.generated.h"

/**
 * AMimicCrawler
 * Mimic variant that clings to ceilings and ventilation shafts.
 * Mimics directional sound to lure players, then drops for ambush.
 */
UCLASS()
class MIMICFACILITY_API AMimicCrawler : public AMimicBase
{
	GENERATED_BODY()

public:
	AMimicCrawler();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;

protected:
	/** Whether this crawler is currently attached to the ceiling. */
	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Mimic|Crawler")
	bool bIsCeilingMounted;
};
