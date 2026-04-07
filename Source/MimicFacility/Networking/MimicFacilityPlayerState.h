// MimicFacilityPlayerState.h — Per-player replicated state: subject number, gear, alive/converted status.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/PlayerState.h"
#include "MimicFacilityPlayerState.generated.h"

UCLASS()
class MIMICFACILITY_API AMimicFacilityPlayerState : public APlayerState
{
	GENERATED_BODY()

public:
	AMimicFacilityPlayerState();

	UFUNCTION(BlueprintCallable, Category = "Player")
	void SetSubjectNumber(int32 Number);

	UFUNCTION(BlueprintPure, Category = "Player")
	int32 GetSubjectNumber() const { return SubjectNumber; }

	UFUNCTION(BlueprintCallable, Category = "Player")
	void MarkConverted();

	UFUNCTION(BlueprintPure, Category = "Player")
	bool IsConverted() const { return bIsConverted; }

protected:
	virtual void BeginPlay() override;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Player")
	int32 SubjectNumber;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Player")
	bool bIsConverted;

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
