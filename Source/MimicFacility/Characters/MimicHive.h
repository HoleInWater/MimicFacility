// MimicHive.h — Merged mass of swarm mimics. Area denial, cannot be contained, multiple voice streams.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "MimicHive.generated.h"

class UAudioComponent;
class UStaticMeshComponent;
class USphereComponent;

UCLASS()
class MIMICFACILITY_API AMimicHive : public AActor
{
	GENERATED_BODY()

public:
	AMimicHive();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;

	UFUNCTION(BlueprintCallable, Category = "Mimic|Hive")
	void AddVoiceProfile(const FString& PlayerID);

	UFUNCTION(BlueprintCallable, Category = "Mimic|Hive")
	void SetGrowthTarget(const FVector& Direction);

	UFUNCTION(BlueprintPure, Category = "Mimic|Hive")
	float GetCurrentRadius() const { return CurrentRadius; }

protected:
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Mimic|Hive")
	TObjectPtr<UStaticMeshComponent> HiveMesh;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Mimic|Hive")
	TObjectPtr<USphereComponent> DenialZone;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Mimic|Hive")
	TObjectPtr<UAudioComponent> HiveAudio;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Mimic|Hive")
	float GrowthRate;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Mimic|Hive")
	float MaxRadius;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Mimic|Hive")
	float DamagePerSecond;

private:
	void GrowHive(float DeltaTime);
	void PlayMultiVoice();

	TArray<FString> AbsorbedVoiceProfiles;
	float CurrentRadius;
	FVector GrowthDirection;
	FTimerHandle VoiceTimer;
};
