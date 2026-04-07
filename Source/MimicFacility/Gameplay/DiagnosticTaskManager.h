// DiagnosticTaskManager.h — Manages the pool of diagnostic tasks per session, tracks completion for win condition.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "DiagnosticTask.h"
#include "DiagnosticTaskManager.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnTaskCompleted, EDiagnosticTaskType, TaskType);
DECLARE_DYNAMIC_MULTICAST_DELEGATE(FOnAllRequiredTasksComplete);

UCLASS()
class MIMICFACILITY_API ADiagnosticTaskManager : public AActor
{
	GENERATED_BODY()

public:
	ADiagnosticTaskManager();

protected:
	virtual void BeginPlay() override;

public:
	UFUNCTION(BlueprintCallable, Category = "Tasks")
	void InitializeSessionTasks(int32 TaskCount = 5);

	UFUNCTION(BlueprintCallable, Category = "Tasks")
	void ActivateNextTask();

	UFUNCTION(BlueprintPure, Category = "Tasks")
	int32 GetCompletedTaskCount() const;

	UFUNCTION(BlueprintPure, Category = "Tasks")
	int32 GetRequiredTaskCount() const { return RequiredCompletions; }

	UFUNCTION(BlueprintPure, Category = "Tasks")
	bool AreRequiredTasksComplete() const;

	UFUNCTION(BlueprintPure, Category = "Tasks")
	ADiagnosticTask* GetActiveTask() const { return ActiveTask; }

	UPROPERTY(BlueprintAssignable)
	FOnTaskCompleted OnTaskCompleted;

	UPROPERTY(BlueprintAssignable)
	FOnAllRequiredTasksComplete OnAllRequiredTasksComplete;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Tasks")
	int32 RequiredCompletions;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Tasks")
	TArray<TSubclassOf<ADiagnosticTask>> TaskClassPool;

private:
	UFUNCTION()
	void HandleTaskStateChanged(EDiagnosticTaskType TaskType, EDiagnosticTaskState NewState);

	UPROPERTY()
	TArray<ADiagnosticTask*> SessionTasks;

	UPROPERTY()
	ADiagnosticTask* ActiveTask;

	int32 CurrentTaskIndex;
	int32 CompletedCount;
};
