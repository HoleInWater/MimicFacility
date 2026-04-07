// MimicSwarm.h — Swarm Mimic variant. Small, fast, uses flocking behavior. Spawns from Trigger Word events.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "MimicBase.h"
#include "MimicSwarm.generated.h"

UCLASS()
class MIMICFACILITY_API AMimicSwarm : public AMimicBase
{
	GENERATED_BODY()

public:
	AMimicSwarm();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;

protected:
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Mimic|Swarm")
	float FlockingRadius;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Mimic|Swarm")
	float SwarmSpeed;
};
