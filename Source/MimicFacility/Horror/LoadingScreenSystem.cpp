// LoadingScreenSystem.cpp — Generates fake database entries with player identity for loading screens.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "LoadingScreenSystem.h"
#include "Online/OnlineSessionNames.h"
#include "OnlineSubsystem.h"
#include "Interfaces/OnlineIdentityInterface.h"
#include "Kismet/GameplayStatics.h"

ULoadingScreenSystem::ULoadingScreenSystem()
{
	bIsActive = false;
	bDataReady = false;
	ElapsedTime = 0.0f;
	TargetDuration = 0.0f;
}

void ULoadingScreenSystem::BeginLoadingScreen()
{
	bIsActive = true;
	ElapsedTime = 0.0f;
	TargetDuration = FMath::RandRange(3.0f, 5.0f);

	CachedDisplayName = ResolvePlayerDisplayName();
	CachedHexCode = GenerateHexCode();
	bDataReady = true;
}

void ULoadingScreenSystem::EndLoadingScreen()
{
	bIsActive = false;
	bDataReady = false;
}

bool ULoadingScreenSystem::ConsumeLoadingData(FString& OutDisplayName, FString& OutHexCode) const
{
	if (!bDataReady) return false;

	OutDisplayName = CachedDisplayName;
	OutHexCode = CachedHexCode;
	return true;
}

FString ULoadingScreenSystem::ResolvePlayerDisplayName() const
{
	IOnlineSubsystem* OnlineSub = IOnlineSubsystem::Get();
	if (OnlineSub)
	{
		IOnlineIdentityPtr Identity = OnlineSub->GetIdentityInterface();
		if (Identity.IsValid())
		{
			FString DisplayName = Identity->GetPlayerNickname(0);
			if (!DisplayName.IsEmpty())
			{
				return DisplayName;
			}
		}
	}

	return TEXT("UNKNOWN SUBJECT");
}

FString ULoadingScreenSystem::GenerateHexCode() const
{
	uint32 Code = FMath::Rand();
	return FString::Printf(TEXT("%08X"), Code);
}
