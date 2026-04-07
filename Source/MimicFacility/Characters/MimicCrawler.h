// MimicCrawler.h — Ceiling Crawler Mimic variant. Uses ceiling-mounted pathfinding and ambush behavior.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "MimicBase.h"
#include "MimicCrawler.generated.h"

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
	UPROPERTY(Replicated, EditAnywhere, BlueprintReadOnly, Category = "Mimic|Crawler")
	bool bIsCeilingMounted;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Mimic|Crawler")
	float DropAttackRange;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
