// DirectorAI.cpp — The Director AI implementation with state machine and fallback dialogue.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "DirectorAI.h"
#include "Networking/MimicFacilityGameState.h"
#include "Kismet/GameplayStatics.h"

ADirectorAI::ADirectorAI()
{
	PrimaryActorTick.bCanEverTick = true;
	CurrentState = EDirectorState::Observing;
	TimeSinceLastDialogue = 0.0f;
	DialogueInterval = 30.0f;
}

void ADirectorAI::BeginPlay()
{
	Super::BeginPlay();

	GetWorldTimerManager().SetTimer(
		StateEvaluationTimer,
		this,
		&ADirectorAI::EvaluateGameState,
		5.0f,
		true
	);

	UE_LOG(LogTemp, Log, TEXT("DirectorAI initialized. State: Observing."));
	Speak(TEXT("Welcome, subjects. The facility is glad to have you."));
}

void ADirectorAI::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	TimeSinceLastDialogue += DeltaTime;
	if (TimeSinceLastDialogue >= DialogueInterval && CurrentState != EDirectorState::Withdrawing)
	{
		TArray<FString> Lines = GetFallbackDialogue(CurrentState);
		if (Lines.Num() > 0)
		{
			int32 Index = FMath::RandRange(0, Lines.Num() - 1);
			Speak(Lines[Index]);
		}
		TimeSinceLastDialogue = 0.0f;
	}
}

void ADirectorAI::SetDirectorState(EDirectorState NewState)
{
	if (CurrentState != NewState)
	{
		EDirectorState OldState = CurrentState;
		CurrentState = NewState;
		UE_LOG(LogTemp, Log, TEXT("Director state: %d -> %d"), static_cast<uint8>(OldState), static_cast<uint8>(NewState));
		OnDirectorStateChanged.Broadcast(OldState, NewState);
	}
}

void ADirectorAI::Speak(const FString& DialogueLine)
{
	UE_LOG(LogTemp, Warning, TEXT("[DIRECTOR]: %s"), *DialogueLine);
	OnDirectorSpeak.Broadcast(DialogueLine);
	TimeSinceLastDialogue = 0.0f;
}

void ADirectorAI::EvaluateGameState()
{
	AMimicFacilityGameState* GS = Cast<AMimicFacilityGameState>(GetWorld()->GetGameState());
	if (!GS) return;

	// State transition logic based on game state
	// This is a simplified version for testing — full implementation will use Claude API
	int32 MimicCount = GS->GetActiveMimicCount();
	int32 ContainedCount = GS->GetContainedMimicCount();

	switch (CurrentState)
	{
	case EDirectorState::Observing:
		if (MimicCount > 0)
		{
			SetDirectorState(EDirectorState::Misleading);
		}
		break;

	case EDirectorState::Misleading:
		if (MimicCount >= 5 || ContainedCount >= 3)
		{
			SetDirectorState(EDirectorState::Escalating);
		}
		break;

	case EDirectorState::Escalating:
		if (MimicCount <= 1 && ContainedCount >= 5)
		{
			SetDirectorState(EDirectorState::Withdrawing);
		}
		break;

	case EDirectorState::Withdrawing:
		// Silence. The Director does not return from this state.
		break;
	}
}

TArray<FString> ADirectorAI::GetFallbackDialogue(EDirectorState State) const
{
	TArray<FString> Lines;

	switch (State)
	{
	case EDirectorState::Observing:
		Lines.Add(TEXT("Reminder: emergency exits are not operational at this time."));
		Lines.Add(TEXT("The facility appreciates your cooperation during this evaluation period."));
		Lines.Add(TEXT("Please continue your exploration. All sectors are currently accessible."));
		Lines.Add(TEXT("Environmental conditions are within acceptable parameters."));
		break;

	case EDirectorState::Misleading:
		Lines.Add(TEXT("I am detecting an anomaly in Sector 7. You may wish to investigate."));
		Lines.Add(TEXT("One of your biosignals appears... irregular. This is likely nothing."));
		Lines.Add(TEXT("The safe route is through the east corridor. I recommend haste."));
		Lines.Add(TEXT("I have confirmed that all personnel in your group are accounted for."));
		break;

	case EDirectorState::Escalating:
		Lines.Add(TEXT("Critical containment failure detected. Please proceed to the nearest checkpoint."));
		Lines.Add(TEXT("I am losing control of several facility subsystems. This was not anticipated."));
		Lines.Add(TEXT("Spore concentrations are approaching hazardous levels in your sector."));
		Lines.Add(TEXT("I strongly advise you do not separate from the group at this time."));
		break;

	case EDirectorState::Withdrawing:
		// Silence.
		break;
	}

	return Lines;
}
