// FacilityControlSystem.h — The Director's interface for controlling the physical facility.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "AI/Weapons/PersonalWeaponSystem.h"
#include "FacilityControlSystem.generated.h"

class AFacilityDoor;
class AFacilityLight;
class ASporeVent;
class APlayerController;

UENUM(BlueprintType)
enum class EFacilityAction : uint8
{
	LockDoor,
	UnlockDoor,
	KillLights,
	RestoreLights,
	ActivateSporeVent,
	DeactivateSporeVent,
	LockdownZone,
	RestoreZone
};

USTRUCT(BlueprintType)
struct FFacilityCommand
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	EFacilityAction Action;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FName TargetTag;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Duration;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Delay;

	FFacilityCommand() : Action(EFacilityAction::LockDoor), Duration(0.0f), Delay(0.0f) {}
};

UCLASS(ClassGroup = (Custom), meta = (BlueprintSpawnableComponent))
class MIMICFACILITY_API UFacilityControlSystem : public UActorComponent
{
	GENERATED_BODY()

public:
	UFacilityControlSystem();

	UFUNCTION(BlueprintCallable, Category = "Facility")
	void ExecuteCommand(const FFacilityCommand& Command);

	UFUNCTION(BlueprintCallable, Category = "Facility")
	void IsolatePlayers(APlayerController* PlayerA, APlayerController* PlayerB);

	UFUNCTION(BlueprintCallable, Category = "Facility")
	void EmotionalManipulation(const FEmotionalProfile& Profile, FName PlayerZone);

	UFUNCTION(BlueprintPure, Category = "Facility")
	TArray<AFacilityDoor*> GetDoorsInZone(FName Zone) const;

	UFUNCTION(BlueprintPure, Category = "Facility")
	TArray<AFacilityLight*> GetLightsInZone(FName Zone) const;

	UFUNCTION(BlueprintPure, Category = "Facility")
	TArray<ASporeVent*> GetVentsInZone(FName Zone) const;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility")
	float PanicFlickerThreshold;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility")
	float DefaultLockdownDuration;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility")
	float IsolationDoorRadius;

protected:
	virtual void BeginPlay() override;

private:
	void ExecuteImmediate(const FFacilityCommand& Command);
	void ScheduleRestore(const FFacilityCommand& Command);

	void LockdownZone(FName Zone);
	void RestoreZone(FName Zone);

	UPROPERTY()
	TArray<TObjectPtr<AFacilityDoor>> AllDoors;

	UPROPERTY()
	TArray<TObjectPtr<AFacilityLight>> AllLights;

	UPROPERTY()
	TArray<TObjectPtr<ASporeVent>> AllVents;

	TMap<FName, FTimerHandle> ActiveLockdownTimers;
};
