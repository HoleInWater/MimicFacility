// FacilityLight.h — Director-controllable light with flicker support.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "FacilityLight.generated.h"

class UPointLightComponent;

UCLASS()
class MIMICFACILITY_API AFacilityLight : public AActor
{
	GENERATED_BODY()

public:
	AFacilityLight();

	UFUNCTION(BlueprintCallable, Category = "Facility|Light")
	void TurnOff();

	UFUNCTION(BlueprintCallable, Category = "Facility|Light")
	void TurnOn();

	UFUNCTION(BlueprintCallable, Category = "Facility|Light")
	void Flicker(float Duration);

	UFUNCTION(BlueprintPure, Category = "Facility|Light")
	bool IsOn() const { return bIsOn; }

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility")
	FName ZoneTag;

protected:
	virtual void BeginPlay() override;
	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Components")
	TObjectPtr<UPointLightComponent> LightComponent;

	UPROPERTY(ReplicatedUsing = OnRep_IsOn, BlueprintReadOnly, Category = "Facility|Light")
	bool bIsOn;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility|Light")
	float DefaultIntensity;

private:
	UFUNCTION()
	void OnRep_IsOn();

	void FlickerTick();
	void StopFlicker();

	FTimerHandle FlickerTimerHandle;
	FTimerHandle FlickerEndHandle;
	bool bWasOnBeforeFlicker;
};
