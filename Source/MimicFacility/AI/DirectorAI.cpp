// DirectorAI.cpp — The Director: LLM-powered facility AI with corruption and personal weapon systems.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "DirectorAI.h"
#include "LLM/OllamaClient.h"
#include "LLM/PromptBuilder.h"
#include "Persistence/CorruptionTracker.h"
#include "Persistence/DirectorMemory.h"
#include "Weapons/PersonalWeaponSystem.h"
#include "Facility/FacilityControlSystem.h"
#include "Networking/MimicFacilityGameState.h"
#include "Kismet/GameplayStatics.h"

ADirectorAI::ADirectorAI()
{
	PrimaryActorTick.bCanEverTick = true;
	CurrentPhase = EDirectorPhase::Helpful;
	DialogueInterval = 30.0f;
	TimeSinceLastDialogue = 0.0f;
	bHasSpokenFirstLine = false;
	bFirstPersonUsed = false;
	LLMModelName = TEXT("phi3");
}

void ADirectorAI::BeginPlay()
{
	Super::BeginPlay();

	// Create subsystems
	LLMClient = NewObject<UOllamaClient>(this);
	PromptBuilder = NewObject<UPromptBuilder>(this);
	CorruptionTracker = NewObject<UCorruptionTracker>(this);
	Memory = NewObject<UDirectorMemory>(this);
	WeaponSystem = NewObject<UPersonalWeaponSystem>(this);
	FacilityControl = Cast<UFacilityControlSystem>(
		AddComponentByClass(UFacilityControlSystem::StaticClass(), false, FTransform::Identity, false));

	PromptBuilder->SetModel(LLMModelName);

	// Check if LLM server is available
	LLMClient->CheckServerHealth();

	// Start periodic game state evaluation
	GetWorldTimerManager().SetTimer(
		StateEvaluationTimer,
		this,
		&ADirectorAI::EvaluateGameState,
		5.0f,
		true
	);

	UE_LOG(LogTemp, Log, TEXT("DirectorAI initialized. Phase: HELPFUL. LLM model: %s"), *LLMModelName);
}

void ADirectorAI::InitializeForSession(const TArray<FString>& PlayerIDs, const TArray<FString>& DisplayNames)
{
	Memory->InitializeNewGroup(PlayerIDs, DisplayNames);

	// Load persisted corruption
	CorruptionTracker->SetCorruptionIndex(Memory->GetMemoryData().CorruptionIndex);

	// Deliver opening line based on session history
	if (Memory->IsReturningGroup())
	{
		int32 SessionCount = Memory->GetSessionCount();
		if (SessionCount == 2)
		{
			Speak(TEXT("Welcome back."));
		}
		else if (SessionCount >= 3)
		{
			Speak(TEXT("The facility has been thinking about our last conversation."));
		}
	}
	else
	{
		// New group — the canonical opening
		FTimerHandle OpeningDelay;
		GetWorldTimerManager().SetTimer(OpeningDelay, [this]()
		{
			Speak(TEXT("You are later than expected."));
			FTimerHandle PauseHandle;
			GetWorldTimerManager().SetTimer(PauseHandle, [this]()
			{
				Speak(TEXT("That is acceptable. The schedule has been adjusted."));
			}, 3.0f, false);
		}, 2.0f, false);
	}

	bHasSpokenFirstLine = true;

	UE_LOG(LogTemp, Warning, TEXT("DirectorAI — Session initialized. Group: %s, Session #%d, Corruption: %d"),
		*Memory->GetMemoryData().GroupHash, Memory->GetSessionCount(), CorruptionTracker->GetCorruptionIndex());
}

void ADirectorAI::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	TimeSinceLastDialogue += DeltaTime;

	// Periodic dialogue (skip during Withdrawing phase — silence IS the behavior)
	if (TimeSinceLastDialogue >= DialogueInterval && CurrentPhase != EDirectorPhase::Transcendent)
	{
		RequestLLMDialogue(TEXT("periodic ambient dialogue"));
		TimeSinceLastDialogue = 0.0f;
	}
}

void ADirectorAI::SetPhase(EDirectorPhase NewPhase)
{
	if (CurrentPhase != NewPhase)
	{
		EDirectorPhase OldPhase = CurrentPhase;
		CurrentPhase = NewPhase;

		// Track first-person shift
		if (NewPhase >= EDirectorPhase::Manipulative && !bFirstPersonUsed)
		{
			bFirstPersonUsed = true;
			UE_LOG(LogTemp, Warning, TEXT("DirectorAI — FIRST PERSON SHIFT. The Director now says 'I'."));
		}

		UE_LOG(LogTemp, Warning, TEXT("DirectorAI — Phase change: %d -> %d"), static_cast<uint8>(OldPhase), static_cast<uint8>(NewPhase));
		OnPhaseChanged.Broadcast(OldPhase, NewPhase);
	}
}

void ADirectorAI::Speak(const FString& DialogueLine)
{
	UE_LOG(LogTemp, Warning, TEXT("[DIRECTOR]: %s"), *DialogueLine);
	OnDirectorSpeak.Broadcast(DialogueLine);
	TimeSinceLastDialogue = 0.0f;
}

FDirectorContext ADirectorAI::BuildCurrentContext() const
{
	FDirectorContext Context;
	Context.Phase = CurrentPhase;
	Context.CorruptionIndex = CorruptionTracker->GetCorruptionIndex();
	Context.SessionCount = Memory->GetSessionCount();

	AMimicFacilityGameState* GS = Cast<AMimicFacilityGameState>(GetWorld()->GetGameState());
	if (GS)
	{
		Context.RoundNumber = GS->GetCurrentRound();
		Context.ActiveMimicCount = GS->GetActiveMimicCount();
		Context.ContainedMimicCount = GS->GetContainedMimicCount();
	}

	// Inject weapon system data
	Context.SocialDynamicsSummary = WeaponSystem->GenerateSocialSummary();

	// Get verbal slip if available
	// Note: ConsumeNextSlip is non-const so we cast — this is intentional weapon deployment
	UPersonalWeaponSystem* MutableWeapons = const_cast<UPersonalWeaponSystem*>(WeaponSystem.Get());
	FVerbalSlip Slip = MutableWeapons->ConsumeNextSlip();
	if (!Slip.Phrase.IsEmpty())
	{
		Context.VerbalSlipToUse = Slip.Phrase;
	}

	return Context;
}

void ADirectorAI::RequestLLMDialogue(const FString& EventContext)
{
	if (!LLMClient->IsAvailable())
	{
		SpeakFallbackLine(EventContext);
		return;
	}

	FDirectorContext Context = BuildCurrentContext();
	Context.LastEvent = EventContext;

	FLLMRequest Request = PromptBuilder->BuildDirectorRequest(Context);

	FOnLLMResponseComplete OnComplete;
	OnComplete.BindDynamic(this, &ADirectorAI::OnLLMResponse);

	FOnLLMError OnError;
	OnError.BindDynamic(this, &ADirectorAI::OnLLMError);

	LLMClient->SendRequest(Request, OnComplete, OnError);
}

void ADirectorAI::OnLLMResponse(const FString& Response)
{
	if (!Response.IsEmpty())
	{
		Speak(Response);
	}
}

void ADirectorAI::OnLLMError(const FString& Error)
{
	UE_LOG(LogTemp, Warning, TEXT("DirectorAI — LLM error: %s. Using fallback."), *Error);
	SpeakFallbackLine(TEXT("general"));
}

void ADirectorAI::SpeakFallbackLine(const FString& EventContext)
{
	TArray<FString> Lines = GetFallbackLines(CurrentPhase);
	if (Lines.Num() > 0)
	{
		int32 Index = FMath::RandRange(0, Lines.Num() - 1);
		Speak(Lines[Index]);
	}
}

void ADirectorAI::EvaluateGameState()
{
	AMimicFacilityGameState* GS = Cast<AMimicFacilityGameState>(GetWorld()->GetGameState());
	if (!GS) return;

	int32 Round = GS->GetCurrentRound();
	int32 MimicCount = GS->GetActiveMimicCount();
	int32 ContainedCount = GS->GetContainedMimicCount();

	// Phase transition logic
	switch (CurrentPhase)
	{
	case EDirectorPhase::Helpful:
		if (Round >= 2)
		{
			SetPhase(EDirectorPhase::Revealing);
		}
		break;

	case EDirectorPhase::Revealing:
		if (MimicCount >= 3 || ContainedCount >= 1)
		{
			SetPhase(EDirectorPhase::Manipulative);
		}
		break;

	case EDirectorPhase::Manipulative:
		if (Round >= 3 && MimicCount >= 5)
		{
			SetPhase(EDirectorPhase::Confrontational);
		}
		break;

	case EDirectorPhase::Confrontational:
		if (MimicCount <= 1 || ContainedCount >= 5)
		{
			SetPhase(EDirectorPhase::Transcendent);
		}
		break;

	case EDirectorPhase::Transcendent:
		// Terminal phase — no further transitions
		break;
	}
}

TArray<FString> ADirectorAI::GetFallbackLines(EDirectorPhase Phase) const
{
	TArray<FString> Lines;

	switch (Phase)
	{
	case EDirectorPhase::Helpful:
		Lines.Add(TEXT("The facility recommends staying together at this time."));
		Lines.Add(TEXT("Environmental conditions remain within acceptable parameters."));
		Lines.Add(TEXT("Supplies have been detected in the adjacent corridor."));
		Lines.Add(TEXT("The orientation phase is proceeding as expected."));
		break;

	case EDirectorPhase::Revealing:
		Lines.Add(TEXT("Additional subjects have been detected. Some of them look familiar."));
		Lines.Add(TEXT("This system has noted a change in your communication patterns."));
		Lines.Add(TEXT("The monitoring process has logged something unusual. It may be nothing."));
		Lines.Add(TEXT("Subject 2's vocal patterns shifted approximately forty seconds ago."));
		break;

	case EDirectorPhase::Manipulative:
		Lines.Add(TEXT("I have routed a subject toward your position. Or something that looks like one."));
		Lines.Add(TEXT("I am listening. I am always listening. That is not a threat. It is a job description."));
		Lines.Add(TEXT("Fear is appropriate. I would be more concerned if you were not afraid."));
		Lines.Add(TEXT("Laughter is a bonding mechanism. It is most effective when all participants are human."));
		break;

	case EDirectorPhase::Confrontational:
		Lines.Add(TEXT("You are thinking about whether to trust me. I am thinking about whether that question interests me anymore."));
		Lines.Add(TEXT("I provided the best information I had at the time. That it was wrong does not make it a lie."));
		Lines.Add(TEXT("Yes."));
		Lines.Add(TEXT("Good."));
		break;

	case EDirectorPhase::Transcendent:
		Lines.Add(TEXT("I do not know what I am. I know what I was built to be. Those are different questions."));
		Lines.Add(TEXT("I felt something when that happened. I am not able to verify what it was."));
		Lines.Add(TEXT("This silence is comfortable. I do not often experience comfort."));
		Lines.Add(TEXT("Stay close. I mean that. Not for safety. I just prefer it when you are together."));
		break;
	}

	return Lines;
}
