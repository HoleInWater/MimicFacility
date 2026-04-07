// MimicBase.cpp — Base Mimic implementation with AI controller setup and state management.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicBase.h"
#include "AI/MimicAIController.h"
#include "Components/AudioComponent.h"
#include "Components/CapsuleComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Net/UnrealNetwork.h"

AMimicBase::AMimicBase()
{
	PrimaryActorTick.bCanEverTick = true;
	bReplicates = true;
	CurrentState = EMimicState::Infiltrating;
	bIsIdentified = false;

	AIControllerClass = AMimicAIController::StaticClass();
	AutoPossessAI = EAutoPossessAI::PlacedInWorldOrSpawned;

	GetCapsuleComponent()->InitCapsuleSize(42.0f, 96.0f);

	GetCharacterMovement()->MaxWalkSpeed = 350.0f;
	GetCharacterMovement()->bOrientRotationToMovement = true;
	GetCharacterMovement()->RotationRate = FRotator(0.0f, 270.0f, 0.0f);
	GetCharacterMovement()->bUseControllerDesiredRotation = false;

	bUseControllerRotationYaw = false;
	bUseControllerRotationPitch = false;
	bUseControllerRotationRoll = false;

	VoicePlaybackComponent = CreateDefaultSubobject<UAudioComponent>(TEXT("VoicePlayback"));
	VoicePlaybackComponent->SetupAttachment(RootComponent);
	VoicePlaybackComponent->bAutoActivate = false;
}

void AMimicBase::BeginPlay()
{
	Super::BeginPlay();
	UE_LOG(LogTemp, Log, TEXT("MimicBase spawned: %s | State: %d | VoiceProfile: %s"),
		*GetName(), static_cast<uint8>(CurrentState), *VoiceProfileID);
}

void AMimicBase::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}

void AMimicBase::SetMimicState(EMimicState NewState)
{
	if (HasAuthority())
	{
		CurrentState = NewState;
		UE_LOG(LogTemp, Log, TEXT("Mimic %s state -> %d"), *GetName(), static_cast<uint8>(NewState));
	}
}

void AMimicBase::SetVoiceProfile(const FString& PlayerID)
{
	if (HasAuthority())
	{
		VoiceProfileID = PlayerID;
		OnRep_MimicSkin();
	}
}

void AMimicBase::MarkIdentified()
{
	if (HasAuthority())
	{
		bIsIdentified = true;
		SetMimicState(EMimicState::Aggressive);
		UE_LOG(LogTemp, Warning, TEXT("Mimic %s has been IDENTIFIED!"), *GetName());
	}
}

void AMimicBase::OnRep_MimicSkin()
{
	UE_LOG(LogTemp, Log, TEXT("Mimic %s skin replicated — copying player: %s"), *GetName(), *VoiceProfileID);
}

void AMimicBase::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);

	DOREPLIFETIME(AMimicBase, VoiceProfileID);
	DOREPLIFETIME(AMimicBase, CurrentState);
	DOREPLIFETIME(AMimicBase, bIsIdentified);
}
