// LoadingScreenSystem.h — Fake loading screens displaying player identity data for psychological effect.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "LoadingScreenSystem.generated.h"

UCLASS(BlueprintType)
class MIMICFACILITY_API ULoadingScreenSystem : public UObject
{
	GENERATED_BODY()

public:
	ULoadingScreenSystem();

	UFUNCTION(BlueprintCallable, Category = "Horror|Loading")
	void BeginLoadingScreen();

	UFUNCTION(BlueprintCallable, Category = "Horror|Loading")
	void EndLoadingScreen();

	bool IsActive() const { return bIsActive; }
	float GetElapsedTime() const { return ElapsedTime; }
	float GetTargetDuration() const { return TargetDuration; }

	bool ConsumeLoadingData(FString& OutDisplayName, FString& OutHexCode) const;

private:
	FString ResolvePlayerDisplayName() const;
	FString GenerateHexCode() const;

	mutable bool bDataReady;
	mutable FString CachedDisplayName;
	mutable FString CachedHexCode;

	bool bIsActive;
	float ElapsedTime;
	float TargetDuration;
};
