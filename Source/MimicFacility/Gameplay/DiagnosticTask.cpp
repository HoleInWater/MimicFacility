// DiagnosticTask.cpp — Base diagnostic task implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "DiagnosticTask.h"
#include "Net/UnrealNetwork.h"

ADiagnosticTask::ADiagnosticTask()
{
	PrimaryActorTick.bCanEverTick = true;
	bReplicates = true;
	TaskState = EDiagnosticTaskState::Inactive;
	RequiredStations = 2;
	VulnerabilityDuration = 15.0f;
	TaskTimeLimit = 120.0f;
	VulnerabilityTimer = 0.0f;
	TaskTimer = 0.0f;
}

void ADiagnosticTask::BeginPlay()
{
	Super::BeginPlay();
}

void ADiagnosticTask::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	if (!HasAuthority()) return;

	if (TaskState == EDiagnosticTaskState::InProgress)
	{
		TaskTimer += DeltaTime;
		if (TaskTimer >= TaskTimeLimit)
		{
			FailTask();
		}
	}

	if (TaskState == EDiagnosticTaskState::VulnerabilityWindow)
	{
		VulnerabilityTimer -= DeltaTime;
		if (VulnerabilityTimer <= 0.0f)
		{
			EndVulnerabilityWindow();
		}
	}
}

void ADiagnosticTask::SetTaskState(EDiagnosticTaskState NewState)
{
	EDiagnosticTaskState OldState = TaskState;
	TaskState = NewState;
	OnTaskStateChanged.Broadcast(TaskType, NewState);
	UE_LOG(LogTemp, Log, TEXT("DiagnosticTask %s: %d -> %d"), *TaskName.ToString(),
		static_cast<uint8>(OldState), static_cast<uint8>(NewState));
}

void ADiagnosticTask::ActivateTask()
{
	if (!HasAuthority() || TaskState != EDiagnosticTaskState::Inactive) return;

	SetTaskState(EDiagnosticTaskState::Available);
	TaskTimer = 0.0f;
	UE_LOG(LogTemp, Warning, TEXT("=== DIAGNOSTIC TASK AVAILABLE: %s ==="), *TaskName.ToString());
}

void ADiagnosticTask::PlayerInteract(const FString& PlayerID, int32 StationIndex)
{
	if (TaskState == EDiagnosticTaskState::Available)
	{
		SetTaskState(EDiagnosticTaskState::InProgress);
	}

	PlayerStationAssignments.Add(PlayerID, StationIndex);
	UE_LOG(LogTemp, Log, TEXT("Task %s: %s at station %d"), *TaskName.ToString(), *PlayerID, StationIndex);
}

void ADiagnosticTask::SubmitReading(const FString& PlayerID, float Value)
{
	PlayerReadings.Add(PlayerID, Value);
	UE_LOG(LogTemp, Log, TEXT("Task %s: %s submitted reading %.2f"), *TaskName.ToString(), *PlayerID, Value);

	// Check if all stations have readings
	if (PlayerReadings.Num() >= RequiredStations)
	{
		BeginVulnerabilityWindow();
	}
}

void ADiagnosticTask::BeginVulnerabilityWindow()
{
	SetTaskState(EDiagnosticTaskState::VulnerabilityWindow);
	VulnerabilityTimer = VulnerabilityDuration;
	OnVulnerabilityWindowOpened.Broadcast(VulnerabilityDuration);
	UE_LOG(LogTemp, Warning, TEXT("Task %s: VULNERABILITY WINDOW OPEN (%.0fs)"),
		*TaskName.ToString(), VulnerabilityDuration);
}

void ADiagnosticTask::EndVulnerabilityWindow()
{
	CompleteTask();
}

void ADiagnosticTask::CompleteTask()
{
	SetTaskState(EDiagnosticTaskState::Completed);
	UE_LOG(LogTemp, Warning, TEXT("=== DIAGNOSTIC TASK COMPLETE: %s ==="), *TaskName.ToString());
}

void ADiagnosticTask::FailTask()
{
	SetTaskState(EDiagnosticTaskState::Failed);
	UE_LOG(LogTemp, Error, TEXT("=== DIAGNOSTIC TASK FAILED: %s ==="), *TaskName.ToString());
}

float ADiagnosticTask::GetVulnerabilityTimeRemaining() const
{
	return FMath::Max(0.0f, VulnerabilityTimer);
}

void ADiagnosticTask::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);
	DOREPLIFETIME(ADiagnosticTask, TaskState);
}
