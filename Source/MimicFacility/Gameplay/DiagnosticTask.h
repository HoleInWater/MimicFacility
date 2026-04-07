// DiagnosticTask.h — Base class for cooperative Round 2+ objectives with split/communicate/commit structure.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "DiagnosticTask.generated.h"

UENUM(BlueprintType)
enum class EDiagnosticTaskState : uint8
{
	Inactive,
	Available,
	InProgress,
	VulnerabilityWindow,
	Completed,
	Failed
};

UENUM(BlueprintType)
enum class EDiagnosticTaskType : uint8
{
	PressureEqualization,
	CircuitRestoration,
	SpecimenLockdown,
	VentilationReroute,
	DataRecovery,
	AcousticCalibration,
	EmergencySealOverride
};

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FOnTaskStateChanged, EDiagnosticTaskType, TaskType, EDiagnosticTaskState, NewState);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnVulnerabilityWindowOpened, float, Duration);

UCLASS(Abstract)
class MIMICFACILITY_API ADiagnosticTask : public AActor
{
	GENERATED_BODY()

public:
	ADiagnosticTask();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;

	UFUNCTION(BlueprintCallable, Category = "Task")
	virtual void ActivateTask();

	UFUNCTION(BlueprintCallable, Category = "Task")
	virtual void PlayerInteract(const FString& PlayerID, int32 StationIndex);

	UFUNCTION(BlueprintCallable, Category = "Task")
	virtual void SubmitReading(const FString& PlayerID, float Value);

	UFUNCTION(BlueprintPure, Category = "Task")
	EDiagnosticTaskState GetTaskState() const { return TaskState; }

	UFUNCTION(BlueprintPure, Category = "Task")
	EDiagnosticTaskType GetTaskType() const { return TaskType; }

	UFUNCTION(BlueprintPure, Category = "Task")
	float GetVulnerabilityTimeRemaining() const;

	UPROPERTY(BlueprintAssignable)
	FOnTaskStateChanged OnTaskStateChanged;

	UPROPERTY(BlueprintAssignable)
	FOnVulnerabilityWindowOpened OnVulnerabilityWindowOpened;

protected:
	virtual void BeginVulnerabilityWindow();
	virtual void EndVulnerabilityWindow();
	virtual void CompleteTask();
	virtual void FailTask();

	void SetTaskState(EDiagnosticTaskState NewState);

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Task")
	EDiagnosticTaskState TaskState;

	UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = "Task")
	EDiagnosticTaskType TaskType;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Task")
	FText TaskName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Task")
	FText TaskDescription;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Task")
	int32 RequiredStations;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Task")
	float VulnerabilityDuration;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Task")
	float TaskTimeLimit;

	float VulnerabilityTimer;
	float TaskTimer;

	UPROPERTY()
	TMap<FString, int32> PlayerStationAssignments;

	UPROPERTY()
	TMap<FString, float> PlayerReadings;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
