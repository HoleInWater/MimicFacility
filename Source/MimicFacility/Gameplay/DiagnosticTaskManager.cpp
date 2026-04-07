// DiagnosticTaskManager.cpp — Diagnostic task pool and session management.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "DiagnosticTaskManager.h"

ADiagnosticTaskManager::ADiagnosticTaskManager()
{
	PrimaryActorTick.bCanEverTick = false;
	bReplicates = true;
	RequiredCompletions = 3;
	ActiveTask = nullptr;
	CurrentTaskIndex = 0;
	CompletedCount = 0;
}

void ADiagnosticTaskManager::BeginPlay()
{
	Super::BeginPlay();
}

void ADiagnosticTaskManager::InitializeSessionTasks(int32 TaskCount)
{
	if (!HasAuthority()) return;

	// Shuffle the task class pool and select TaskCount tasks
	TArray<TSubclassOf<ADiagnosticTask>> ShuffledPool = TaskClassPool;

	// Fisher-Yates shuffle
	for (int32 i = ShuffledPool.Num() - 1; i > 0; i--)
	{
		int32 j = FMath::RandRange(0, i);
		ShuffledPool.Swap(i, j);
	}

	int32 SpawnCount = FMath::Min(TaskCount, ShuffledPool.Num());
	for (int32 i = 0; i < SpawnCount; i++)
	{
		if (ShuffledPool[i])
		{
			FActorSpawnParameters SpawnParams;
			ADiagnosticTask* Task = GetWorld()->SpawnActor<ADiagnosticTask>(ShuffledPool[i], GetActorLocation(), FRotator::ZeroRotator, SpawnParams);
			if (Task)
			{
				Task->OnTaskStateChanged.AddDynamic(this, &ADiagnosticTaskManager::HandleTaskStateChanged);
				SessionTasks.Add(Task);
			}
		}
	}

	CurrentTaskIndex = 0;
	CompletedCount = 0;

	UE_LOG(LogTemp, Warning, TEXT("DiagnosticTaskManager — Initialized %d tasks for session (need %d to win)"),
		SessionTasks.Num(), RequiredCompletions);
}

void ADiagnosticTaskManager::ActivateNextTask()
{
	if (!HasAuthority()) return;

	if (CurrentTaskIndex >= SessionTasks.Num())
	{
		UE_LOG(LogTemp, Warning, TEXT("DiagnosticTaskManager — No more tasks available"));
		return;
	}

	ActiveTask = SessionTasks[CurrentTaskIndex];
	ActiveTask->ActivateTask();
	CurrentTaskIndex++;
}

void ADiagnosticTaskManager::HandleTaskStateChanged(EDiagnosticTaskType TaskType, EDiagnosticTaskState NewState)
{
	if (NewState == EDiagnosticTaskState::Completed)
	{
		CompletedCount++;
		OnTaskCompleted.Broadcast(TaskType);
		UE_LOG(LogTemp, Warning, TEXT("DiagnosticTaskManager — Task complete! (%d/%d)"), CompletedCount, RequiredCompletions);

		if (AreRequiredTasksComplete())
		{
			OnAllRequiredTasksComplete.Broadcast();
			UE_LOG(LogTemp, Warning, TEXT("=== ALL REQUIRED TASKS COMPLETE — EXIT AVAILABLE ==="));
		}
		else if (CurrentTaskIndex < SessionTasks.Num())
		{
			// Auto-activate next task after a delay
			FTimerHandle TimerHandle;
			GetWorldTimerManager().SetTimer(TimerHandle, this, &ADiagnosticTaskManager::ActivateNextTask, 10.0f, false);
		}

		ActiveTask = nullptr;
	}
	else if (NewState == EDiagnosticTaskState::Failed)
	{
		UE_LOG(LogTemp, Warning, TEXT("DiagnosticTaskManager — Task failed. Activating next."));
		ActiveTask = nullptr;
		ActivateNextTask();
	}
}

int32 ADiagnosticTaskManager::GetCompletedTaskCount() const
{
	return CompletedCount;
}

bool ADiagnosticTaskManager::AreRequiredTasksComplete() const
{
	return CompletedCount >= RequiredCompletions;
}
