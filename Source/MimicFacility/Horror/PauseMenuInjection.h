// PauseMenuInjection.h — Injects player's own recorded phrases into the pause menu as fake debug text.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "PauseMenuInjection.generated.h"

UCLASS(BlueprintType)
class MIMICFACILITY_API UPauseMenuInjection : public UObject
{
	GENERATED_BODY()

public:
	UPauseMenuInjection();

	UFUNCTION(BlueprintCallable, Category = "Horror|PauseMenu")
	void FeedPlayerPhrase(const FString& Phrase);

	UFUNCTION(BlueprintCallable, Category = "Horror|PauseMenu")
	void InjectIntoPauseMenu();

	bool ConsumePhrases(TArray<FString>& OutPhrases) const;

private:
	UPROPERTY()
	TArray<FString> RecentPhrases;

	mutable bool bInjectionPending;
	mutable TArray<FString> PendingPhrases;

	static constexpr int32 MaxStoredPhrases = 20;
	static constexpr int32 DisplayCount = 3;
};
