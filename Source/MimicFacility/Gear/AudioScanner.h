// AudioScanner.h — Handheld device that analyzes nearby voice audio. Shows waveform glitches on mimics.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GearBase.h"
#include "AudioScanner.generated.h"

USTRUCT(BlueprintType)
struct FScanResult
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly)
	FString TargetID;

	UPROPERTY(BlueprintReadOnly)
	bool bIsMimic;

	UPROPERTY(BlueprintReadOnly)
	float WaveformIntegrity;

	UPROPERTY(BlueprintReadOnly)
	float ScanTimestamp;

	FScanResult() : bIsMimic(false), WaveformIntegrity(1.0f), ScanTimestamp(0.0f) {}
};

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnScanComplete, const FScanResult&, Result);

UCLASS()
class MIMICFACILITY_API AAudioScanner : public AGearBase
{
	GENERATED_BODY()

public:
	AAudioScanner();

	virtual void Activate() override;

	UFUNCTION(BlueprintPure, Category = "Gear|Scanner")
	FScanResult GetLastScanResult() const { return LastResult; }

	UFUNCTION(BlueprintPure, Category = "Gear|Scanner")
	bool IsScanning() const { return bIsScanning; }

	UPROPERTY(BlueprintAssignable)
	FOnScanComplete OnScanComplete;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Gear|Scanner")
	float ScanRange;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Gear|Scanner")
	float ScanDuration;

protected:
	virtual void BeginPlay() override;

private:
	void PerformScan();
	void CompleteScan();

	bool bIsScanning;
	FScanResult LastResult;
	FTimerHandle ScanTimer;
};
