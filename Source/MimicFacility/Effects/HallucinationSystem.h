// HallucinationSystem.h — Spore-driven hallucination system that distorts player perception based on exposure.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "HallucinationSystem.generated.h"

class UPostProcessComponent;
class ASporeFilter;

UENUM(BlueprintType)
enum class EHallucinationType : uint8
{
	AudioDistortion,
	VisualFlicker,
	ShadowMovement,
	FalsePlayerEcho,
	EnvironmentalShift
};

USTRUCT(BlueprintType)
struct FHallucinationEvent
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EHallucinationType Type;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, meta = (ClampMin = "0.0", ClampMax = "1.0"))
	float Intensity;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Duration;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector SourceLocation;

	float ElapsedTime;
};

UCLASS(ClassGroup=(Custom), meta=(BlueprintSpawnableComponent))
class MIMICFACILITY_API UHallucinationSystem : public UActorComponent
{
	GENERATED_BODY()

public:
	UHallucinationSystem();

	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

	UFUNCTION(BlueprintCallable, Category = "Effects|Hallucination")
	void AddSporeExposure(float Amount, float DeltaTime);

	UFUNCTION(BlueprintCallable, Category = "Effects|Hallucination")
	void TriggerHallucination(EHallucinationType Type, float Intensity, float Duration, FVector SourceLocation);

	UFUNCTION(BlueprintPure, Category = "Effects|Hallucination")
	float GetSporeExposure() const { return SporeExposure; }

protected:
	virtual void BeginPlay() override;

private:
	void ProcessActiveHallucinations(float DeltaTime);
	void UpdateSporeThresholds();
	void ApplyPostProcessEffects();

	UPROPERTY()
	TObjectPtr<UPostProcessComponent> PostProcessComp;

	UPROPERTY(VisibleAnywhere, Category = "Effects|Hallucination")
	float SporeExposure;

	UPROPERTY(EditDefaultsOnly, Category = "Effects|Hallucination")
	float ExposureDecayRate;

	TArray<FHallucinationEvent> ActiveHallucinations;
};
