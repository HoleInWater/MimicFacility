// SignalJammer.h — Portable device that blocks The Director's intercoms in a radius. Battery-limited.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GearBase.h"
#include "SignalJammer.generated.h"

class USphereComponent;

UCLASS()
class MIMICFACILITY_API ASignalJammer : public AGearBase
{
	GENERATED_BODY()

public:
	ASignalJammer();

	virtual void Activate() override;
	virtual void Tick(float DeltaTime) override;

	UFUNCTION(BlueprintPure, Category = "Gear|Jammer")
	bool IsJamming() const { return bIsActive; }

	UFUNCTION(BlueprintPure, Category = "Gear|Jammer")
	float GetBatteryRemaining() const { return BatteryLife; }

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Gear|Jammer")
	float JamRadius;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Gear|Jammer")
	float MaxBatteryLife;

protected:
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Gear|Jammer")
	TObjectPtr<USphereComponent> JamZone;

private:
	bool bIsActive;
	float BatteryLife;
};
