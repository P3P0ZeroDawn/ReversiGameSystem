# Account and input validation rules

This document records the rules used by the WPF client and the checks required when account services are added. Update it when a use case introduces or changes an input rule. The client currently demonstrates account flows without a connected account server; a success dialog in the prototype does not prove that data was saved remotely.

## Shared account fields

- Registration requires a username, email address, password, and password confirmation. A profile picture is optional.
- Leading and trailing whitespace is removed from usernames and email addresses before validation or display. Passwords are compared and validated exactly as entered; they are never trimmed.
- Empty or whitespace-only required fields are rejected. A canceled dialog leaves the current page or account preview unchanged.
- The server must repeat applicable validation before creating or changing an account. Client validation is an early user-facing check, not an authorization or uniqueness guarantee.

## Username

Used by CU-01 account creation and CU-04 username modification. Implemented in `Validation/UsernamePolicy.cs`.

- After trimming, require 3 to 32 UTF-16 code units, inclusive.
- The first character must be a letter or digit. Remaining characters may be letters, digits, or underscores (`_`). Spaces, hyphens, and other punctuation are rejected.
- The code uses .NET `char.IsLetterOrDigit`, so letters and digits from supported Unicode scripts are accepted. Supplementary characters represented by surrogate pairs are currently rejected.
- Empty or whitespace-only input has its own required-field message. Other failures use the invalid-format message. The same policy applies in registration and username modification.
- A server must check uniqueness against other active accounts and commit the change before reporting success. Whether names that differ only by case or Unicode normalization count as duplicates is **not yet defined**; the client cannot decide this alone. The previous name must remain intact after a failed save.
- The current profile dialog updates only `AccountPreviewState`, which lasts for the current application session. After sign-in, the current username may show as unavailable until account retrieval is connected. The occupied-name, connection, and persistence messages exist in both resource dictionaries but cannot be triggered by a server response yet.

## Email address

Used in registration, sign-in, and the first step of password reset. Implemented in `Validation/EmailPolicy.cs`.

- Accept exactly one `@` between an unquoted ASCII dot-atom local part and a complete domain.
- The local part may contain ASCII letters, digits, dots, and ``!#$%&'*+-/=?^_`{|}~``. Dots may not start or end the local part or appear consecutively. Accented letters are not accepted in the local part.
- Limit the local part to 64 characters and the complete address to 254 characters, including after converting an internationalized domain to ASCII.
- Convert internationalized domains with `IdnMapping`. Require at least two labels, a final label of at least two characters, labels of at most 63 ASCII characters, and a domain of at most 253 ASCII characters. Labels may contain ASCII letters, digits, and internal hyphens.
- Syntax validation cannot establish that a mailbox exists or receives mail. Registration ownership and account lookup require server verification.

## Password and confirmation

Used in registration, password reset, and CU-05 password modification. Implemented in `Validation/PasswordPolicy.cs`.

- Require 8 to 128 Unicode code points, inclusive. Count valid surrogate pairs as one code point.
- Require at least one uppercase letter, one lowercase letter, one decimal digit, and one punctuation or symbol character. `!`, `*`, and emoji can satisfy the symbol requirement; spaces cannot.
- Allow spaces and other supported Unicode characters. Reject control characters, invisible format characters, line and paragraph separators, and unmatched surrogates.
- Password and confirmation must match exactly. Missing, mismatch, length, unsupported-character, and missing-character-class cases have localized alerts.
- CU-05 also requires a nonblank current password. The client must never infer whether it is correct from local state; only the account server can verify it. The server must replace the old password securely before the client shows success. Cancellation or any failed operation leaves the old password valid.
- The future server must enforce the applicable policy, reject blocked passwords, rate-limit authentication attempts, and store passwords with an appropriate salted password hash. The original CU-01 policy also calls for NFC normalization before hashing; this must be coordinated with confirmation and sign-in behavior before implementation. None of these server checks is present in the WPF prototype.

The 8-character minimum and character-class requirement are project choices. Revisit them if the finished authentication system must comply with a particular external password standard.

## Profile picture

Used by CU-06 from account creation and My profile. Implemented in `Validation/ProfilePicturePolicy.cs` and `Services/ProfileImageSelectionService.cs`.

- The picture is optional during registration. The file must have a `.png`, `.jpg`, or `.jpeg` extension and decode as PNG or JPEG.
- Require a nonempty file of at most 5 MiB (5,242,880 bytes). Width and height must each be from 1 to 4,096 pixels.
- The client checks the extension, file size, decoded image format, and dimensions before asking the player to confirm the selection. An invalid image shows an alert and leaves the previous selection intact. Closing the file picker or declining confirmation also leaves it intact.
- In account creation, a confirmed image is staged and previewed. It must be uploaded with the account creation request after an account exists. The current prototype carries the preview into the temporary session, but does not upload the image.
- In My profile, the new image must replace the visible and stored picture only after the account service reports a successful save. The current service boundary reports unavailable, so the previous picture remains visible. Connection and persistence failures have separate localized alerts.
- The future server must validate the upload again, re-encode it, and assign a generated storage name before saving it.

## Verification codes and other text dialogs

- Registration, sign-in, and password reset code dialogs accept exactly six ASCII digits (`0`–`9`). The dialog filters other characters and enables confirmation only at six digits. Code generation, delivery, expiration, attempt limits, and verification require the server.
- Guest entry uses the policy and temporary-session boundary documented under CU-28.
- Joining a match by code requires exactly six characters from `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`. Input is trimmed and converted to uppercase before validation, so lowercase entry is accepted. `I`, `O`, `0`, and `1` are excluded to reduce transcription mistakes. An invalid or unavailable code leaves the dialog flow active and does not modify a match.
- Password reset accepts a valid email, then a six-digit code, then a new password and exact confirmation under the shared password policy. The WPF flow currently shows the dialogs; sending or verifying the code and saving the password require the server.
- Language changes require confirmation through `LanguageChangeService`. Canceling keeps the existing language.

## Interface sizing and scrolling

- The main navigation window is limited to the current Windows work area and supports a minimum
  client size of 640 by 560 device-independent units. Every page provides vertical scrolling so
  actions remain reachable when the available height is reduced.
- Pages with intentionally wide content, including the game, profile, settings, and match-detail
  views, provide horizontal scrolling. Narrower forms wrap localized explanatory text inside their
  cards instead of assigning fixed text heights.
- Reusable dialog windows are limited to the current work area. Their content scrolls vertically
  and horizontally when a localized message, display scaling, or a small screen would otherwise
  place fields or actions outside the visible area.
- The active game removes both the chat panel and its layout columns for a guest session. The board
  and player information are recentered in the available space, and no chat content or control is
  created as a visible substitute.

## Guest access

CU-28 starts from Login, where the user has no account session. The use-case text also names Main menu as its precondition and trigger, which conflicts with the no-session precondition and with the existing navigation; the client keeps the guest action on Login and opens Main menu only after a temporary session is created.

- The supplied name is trimmed, then validated by `GuestUsernamePolicy` with the shared account-name rules: 3 to 32 UTF-16 code units; a letter or digit first; and only letters, digits, or underscores after that. The user supplies only the base name.
- The temporary identity stores only the normalized base name. `GuestAccess_DisplayNameFormat` adds a localized visual prefix: `Pedro` appears as `Guest-Pedro` in English and `Invitado-Pedro` in Spanish. Changing language updates the displayed form without changing the session identifier, match participants, turns, movements, or results.
- `GuestSessionStore` compares normalized base names with `StringComparer.OrdinalIgnoreCase`. While `Pedro` is active, another `pedro` request is rejected regardless of the interface language. Signing out ends the session and releases the name. A deployed server must enforce this uniqueness across all connected clients.
- The access dialog states that the identity is temporary, statistics are not persisted, and chat and all friendship actions are unavailable. Canceling creates no session. Missing, invalid, occupied, and session-creation outcomes use distinct localized alerts; correctable name errors reopen the dialog with the entered value preserved.
- A successful start creates only an in-memory `GuestSession`; it clears any account preview before navigation and never creates an account record. The guest main menu hides friends, profile, match history, and account settings. Game pages remove the complete chat panel and recenter the remaining game content, while create and join match flows retain the language-independent base name.

## Player statistics

CU-07 is displayed inside a profile through `Views/PlayerStatisticsControl.xaml`; it is not a separate page. The control accepts a player identifier for a public profile. A null identifier selects the authenticated player's own account. Currently only My profile opens this control because public-profile navigation has not yet been connected.

- Count only completed matches associated with the selected player. Each completed match contributes exactly one win, loss, or draw from that player's perspective.
- Matches played equals wins plus losses plus draws. Win rate equals wins divided by matches played; draws remain in the denominator. Display it as a localized percentage with one decimal place.
- When a successful query returns no completed matches, show zero for every count and a zero win rate. Before a successful query, and after a connection or retrieval failure, show unavailable values instead of fabricated zeros.
- The query and its errors are read-only. The current service boundary reports a connection failure until the server can supply finished match outcomes.

## Game appearance preferences and board background

The signed-in player opens `Views/SettingsPage.xaml` from Options in the main menu. The option is hidden for guests because CU-08 associates the background with an account.

- `Use profile pictures on game pieces` and `Use a custom board background` affect only the player's own game view. The opponent's view must use that opponent's preferences. Both switches are held in `UserPreferencesState` during the session and reset on sign-out or a new sign-in. `GamePage` now renders these preferences locally: it uses `PieceMask.png` for circular profile-image pieces and overlays `BoardMask.png` on a custom background so coordinates and the grid remain visible. Loading and synchronizing stored preferences still require the account server.
- The custom background switch may be enabled before an image has been saved. In that state, the board must continue to use its default background. A successful background save turns the switch on; the player may later turn it off without deleting the saved image.
- CU-08 accepts a nonempty `.png`, `.jpg`, or `.jpeg` file that decodes as PNG or JPEG, is at most 5 MiB (5,242,880 bytes), and is from 1 to 4,096 pixels on each side. This reuses the profile-image format and size policy with a larger local preview.
- Closing the file picker, selecting an invalid image, or declining confirmation leaves the previous image and switch state unchanged. A save failure also leaves the prior background and configuration intact.
- The future account service must validate, re-encode, and store the image as a preference of the authenticated account. It must return success only after the new image is saved. The current service boundary reports unavailable, so a new selection is not applied or claimed as saved.

## Interface language

CU-09 supports English (`en-US`) and Spanish (`es-MX`). The language selector is part of each current page header, including Login and the main menu; it is not a separate settings page. The use case lists a signed-in session as a precondition while explicitly naming Login as a trigger, so the client permits selection before sign-in to keep Login readable.

- Selecting the active language does nothing. Selecting another language asks for confirmation. Declining restores the previous selection.
- After confirmation, the client checks that the culture is supported and that its resource set contains every default resource key before changing the active culture. Unsupported or incomplete resources preserve the previous language and show a localized error.
- A successful change updates the shared UI culture and localized bindings on the current and subsequent screens. The choice currently lasts for the application session; saving it as an account preference and reporting persistence failures require the future account service.

## Account deactivation

CU-10 starts from the authenticated player's own profile. It is an account state change, not account deletion.

- Ask for explicit confirmation before sending a request. Canceling leaves the account and session unchanged.
- The server must reject deactivation while the player is in an active match. It must mark the authenticated account inactive without physically deleting it and preserve historical matches, moves, messages, and results.
- Only a confirmed server success ends the local session, shows the success alert, and returns to Login. An active-match response, connection failure, or persistence failure leaves the account active and the player on the profile page.
- The current client has no account service. Its service boundary reports connection failure, so a confirmation cannot deactivate an account or sign the player out yet.

## Match creation and initial board

CU-11 starts from Create match in the main menu and is available to authenticated players and guests.

- The default turn duration is 40 seconds. A custom value must be an ASCII whole number from 10 through 120 seconds, inclusive. Empty values, decimals, signs, letters, and values outside the range are invalid and show the localized invalid-time alert.
- A newly created temporary match receives a six-character code under `MatchCodePolicy`, using uppercase letters and digits while excluding `I`, `O`, `0`, and `1`. The client keeps codes unique among active temporary matches and invalidates a code when the match starts against the system, a second participant joins, the waiting screen is canceled, or the active match is left. The match server must generate and enforce globally unique codes in the deployed system.
- The initial board is always 8 × 8. White pieces occupy D4 and E5; black pieces occupy E4 and D5. Each player therefore begins with two pieces, and the creator uses black and takes the first turn.
- The waiting screen exposes the match code, cancellation, and the option to play against the system. Starting against the system changes the temporary match from Waiting to In progress and opens `GamePage`. A remote opponent connection, server ownership of the code, and match persistence remain pending.
- `GamePage` shows both participants, scores derived from the board state, the creator's turn,
  configured time, board, the chat area for registered accounts, and the localized instructions
  defined under CU-20.
- The game interface exposes an abandonment action; its complete state and error rules are documented under CU-17.
- CU-16 extends the initial board with legal-move handling, turn countdown, automatic timeout moves, passes, ordinary match completion, and a legal random move for the prototype system opponent. Network synchronization, authoritative server timing, and a competitive system-opponent strategy remain pending.
- `MatchCreationService` currently creates session-only prototype state so the GUI flow can be exercised. It does not claim that a server match was persisted. The production adapter must create the match atomically, register the creator, preserve no partial match after failure, and map connection and persistence failures to their separate localized alerts.

## Match information, spectating, and replay

CU-12 uses one read-only `MatchInformationPage` for current and historical matches. The page can be opened in spectator mode from Available matches or in replay mode from Previous matches.

- Both modes show the two participants and their assigned colors, match state, configured time per turn, board state, piece counts, registered moves, and final result when one exists. An in-progress match explicitly states that it has no definitive result.
- Spectator mode displays the latest valid board and hides replay controls. It provides no move, chat, or abandonment actions because the viewer is not a participant. Live server updates remain pending.
- Replay mode starts at the standard initial board and provides Previous, Play/Pause, and Next controls over the recorded sequence. The selected movement is highlighted, the board and piece counts update for every frame, and playback stops at the last registered state.
- `ReversiBoardControl` is shared by the active game, spectator view, and replay. It applies the same local profile-piece and custom-background preferences without changing another player's view.
- Previous matches are available only for an authenticated account; the option is hidden in the guest menu. The current prototype supplies deterministic session records so the GUI and replay can be exercised. The future match service must return only matches authorized for the requesting player.
- A missing match, connection failure, or retrieval error prevents navigation to match information and shows its corresponding localized alert. These error responses and authoritative match data remain pending in the server adapter.
- The active game, Previous matches, and Match information screens include the shared English/Spanish selector and require the same confirmation before changing language. Dynamic scores, states, movement descriptions, and replay controls refresh after a successful change.

## Match history

CU-18 is available only from an authenticated player's main menu. Guest sessions do not expose the history action.

- `TryGetHistory` includes only records in which the authenticated player's name matches one of the two participants. The production server must enforce this rule using the authenticated account identifier rather than a display name.
- Records are ordered by `RecordedAtUtc` from newest to oldest before the page receives them. Each row shows the opponent, localized date and time, state, result, current or final score with the authenticated player's score first, and end reason when the match has one.
- A finished match is classified as a victory, defeat, or draw relative to the authenticated player. A match still in progress has no final result or end reason and shows its current score.
- An empty successful response keeps the page open, displays the localized empty-history message, and leaves the Back action available.
- Selecting a record starts CU-12 in replay mode and passes its match identifier through the retrieval boundary. Back returns to the authenticated main menu.
- Connection and retrieval failures clear the list and use separate localized alerts. The prototype returns successful deterministic data; authoritative history retrieval and failure responses remain pending in the server adapter.

## Movement history

CU-19 starts from a match-information view that the player is already authorized to access.

- View movement details sends the selected match identifier to `MovementHistoryService`. The service returns only that match's registered movements and sorts them by movement number in ascending sequence.
- Every detailed row shows the sequence number, player, board position, localized date and time, number of opponent pieces converted by the movement, and whether its origin was manual or automatic. The placed piece is not included in the affected-piece count. Timeout and system-selected movements are both presented as automatic.
- A successful response with no movements opens the same dialog with a localized empty-history message and keeps the Close action available.
- Closing the dialog returns to the unchanged match-information view. The compact movement list remains available for replay frame selection.
- A connection or retrieval failure opens no movement dialog and uses its dedicated localized error alert, so no partial list is displayed.
- The current boundary reads the already-authorized prototype match. The server adapter must verify access using immutable account and match identifiers and return authoritative movement timestamps, affected-piece counts, and origins.

## Reversi instructions

CU-20 is available from the main menu and from the active game.

- `InstructionsService` reads the title and complete rules from the currently active localization dictionary when the player requests them. It rejects a missing, blank, inaccessible, or incorrectly formatted resource rather than displaying partial instructions.
- The shared rules explain capture directions, the standard initial coordinates and first color, configured turn timing, skipped turns, the three-timeout inactivity loss, ordinary and exceptional end conditions, draws, and chat conduct.
- Opening instructions from `GamePage` prefixes the shared rules with the local player's piece color and that match's configured seconds per turn. Opening them from the main menu displays the same rules without match-specific values.
- The informational alert is modal and owned by the originating window. Closing it reveals the same page and leaves the account, match, board, timer, movements, and preferences unchanged.
- If instruction resources cannot be loaded, the client shows the localized `Instructions_LoadingError` alert and preserves the originating page and all prior state.

## Friendship requests

CU-21 is available only to an authenticated account. Guest menus continue to hide the complete friends section.

- The main-menu Add friend action opens `AddFriendsPage`. Its username search trims the query, compares without case sensitivity, returns only active accounts, excludes the authenticated account, and orders matching usernames for stable presentation. An empty query lists all other active prototype accounts.
- Every result uses the circular profile-picture presentation shared with account creation and account information. Selecting the username opens that player's public profile; selecting Send request starts the same operation directly from the search result.
- A public `AccountInformationPage` hides email, account modification, password, profile-picture modification, deactivation, and other private actions. It shows Send friendship request and returns to the player search that opened it. The authenticated player's own profile keeps its existing private controls and never displays that action.
- Before creating a request, `FriendshipStore` checks the current and recipient usernames, rejects the same account, verifies that the recipient is still active, rejects an accepted friendship, and rejects any pending request between the same pair in either direction.
- The unordered relationship key makes accepted friendships and pending requests unique regardless of sender order. All checks and insertion occur under one lock; only a completely successful operation adds one pending request.
- Success identifies the recipient in a localized confirmation. Self-addition, existing friendship, existing pending request, unavailable account, connection failure, and persistence failure each use a distinct localized alert.
- The current store contains deterministic session-only accounts and relationships so every client branch can be exercised. The server must repeat the checks atomically using authenticated account identifiers and leave no partial or duplicate request after failure.

## Received friendship requests

CU-22 is available from the friends section of the authenticated main menu. The
entire section remains hidden for guest sessions.

- `FriendRequestsService` returns only requests whose recipient matches the
  authenticated username and whose state is Pending. Username comparison is
  case-insensitive.
- Requests are ordered from the newest to the oldest. Each row displays the
  requester's username, a circular profile image, and the received date and
  time formatted with the active interface culture.
- A successful empty response displays the localized empty-state text and
  keeps the Back action available. It is distinct from a retrieval failure.
- A connection or retrieval failure clears the list before displaying its
  specific localized alert, so stale or partial requests are never shown.
- Rows remain selectable by their immutable request identifier. CU-23 will
  consume that selection when its accept and reject actions are defined.
- The current service reads deterministic session-only requests. The server
  adapter must authenticate the recipient and return authoritative pending
  states and creation timestamps.

## Friendship request responses

CU-23 starts when the authenticated player selects a pending row in
`FriendRequestsPage`.

- The response dialog identifies the requester and received date, then offers
  Accept, Reject, and Cancel as separate localized actions. Cancel closes the
  dialog without changing the request.
- `FriendRequestResponseService` rechecks the request state before checking
  the authenticated recipient. A missing or non-pending identifier is treated
  as already processed, and a pending request for another account is rejected
  as unauthorized.
- Acceptance adds the unordered friendship key and changes the request to
  Accepted while holding the same store lock. Rejection changes only the
  request state to Rejected. Both paths remove the item from the pending list.
- Success, already-processed, unauthorized, connection, and persistence
  outcomes have distinct localized feedback. The list refreshes after either
  successful response and after an already-processed response.
- The in-memory operation is atomic. The server adapter must repeat the state,
  recipient, friendship, and persistence changes in one transaction so a
  failure retains the previous request state and creates no partial friendship.

## Friends list

CU-24 is presented in the friends panel of the authenticated main menu, which
is the existing friends area of the interface. Guest sessions do not load or
display this panel.

- `FriendsService` returns only active accounts connected to the authenticated
  username by an accepted relationship key. Relationship lookup is symmetric,
  excludes the current account, and orders usernames using the active culture.
- Each row displays the friend's username and circular profile image. The View
  action opens that player's public `AccountInformationPage`; private account
  fields and the redundant Send request action remain hidden.
- A successful result without relationships displays the localized empty state
  inside the menu. Connection or retrieval failures clear the list before
  displaying their distinct localized alerts.
- Friendships accepted through CU-23 use the same session store and therefore
  appear when the main menu is shown again. CU-25 can use the same row and
  relationship identifier for removal.
- The server adapter must authenticate the account and return only valid,
  accepted relationships with the public profile information the player may
  view.

## Friendship removal

CU-25 starts from the Remove action beside a friend in the authenticated main
menu's friends panel.

- A localized confirmation names the selected friend and provides explicit
  Remove friend and Cancel actions. Cancel leaves the relationship unchanged.
- `FriendRemovalService` builds the relationship key from the authenticated
  username and selected friend. It can therefore remove only a relationship
  that includes the current player; a missing or previously removed key
  returns NotFound and refreshes the list.
- Successful removal deletes only the accepted relationship from the session
  store. Match records, movements, results, chats, accounts, and resolved
  friendship requests are stored separately and are not modified.
- Success, missing relationship, connection failure, and persistence failure
  each have distinct localized feedback. The list refreshes only after success
  or NotFound; failure responses preserve the displayed relationship.
- The server must authenticate the player, recheck relationship membership and
  active state, and deactivate the relationship transactionally. Historical
  data must remain associated with its original accounts.

## Chat messages and moderation

CU-26 starts from the chat composer in an active `GamePage` conversation.

- The client trims leading and trailing whitespace. A message must contain at
  least one character and no more than 200 UTF-16 code units after trimming.
  Empty and oversized values are rejected without creating a message.
- Only an authenticated participant in a match that remains InProgress may
  send. Guest sessions hide the complete chat panel and are also rejected at
  the service boundary.
- Moderation compares complete words or expressions without case or diacritics
  and treats punctuation as a separator. The current supported expressions are
  `idiot`, `idiota`, `moron`, `dumbass`, `stupid`, `estupido`, `estupida`,
  `imbecil`, `pendejo`, `pendeja`, `mierda`, `puta`, `puto`, `fuck`,
  `shut up`, and `callate`. Substrings inside an otherwise different word are
  not blocked.
- A prohibited message is never stored or displayed. Its infringement records
  the player, conversation, matched rule, and UTC timestamp. The third
  infringement for the same account within a rolling 24-hour window creates a
  10-minute mute. Additional attempts during the mute create neither messages
  nor new infringements and show the localized mute end time.
- `ChatStore` performs mute lookup, moderation, infringement registration,
  optional mute creation, and message storage under one lock. A successful
  message receives an identifier, conversation, sender, trimmed content, and
  UTC timestamp.
- `GamePage` appends and clears the composer only after Success. Empty,
  unauthorized, muted, blocked, connection, message persistence, moderation
  persistence, and mute persistence outcomes retain the entered text and use
  distinct localized feedback.
- The current store is session-only. The server must authorize the participant,
  evaluate account-wide moderation, and persist the message or moderation
  result transactionally before broadcasting it to authorized participants.

## Chat message reading

CU-27 starts when an authenticated participant opens the chat contained in an
active `GamePage`.

- `ChatService.TryGetMessages` repeats the same participant, guest-session, and
  InProgress authorization checks used by message sending. An unauthorized
  result always returns an empty collection.
- The store selects only messages whose conversation identifier exactly matches
  the requested match code and orders them by UTC timestamp before returning
  them. Each message retains its sender, content, timestamp, and identifier.
- A successful conversation without messages displays the localized empty
  state. Authorization, connection, or retrieval failure clears the list and
  never presents a partial history as complete.
- While the page remains open, it subscribes to stored-message notifications.
  Each received message is rechecked against the active conversation and
  current authorization, deduplicated by identifier, inserted by timestamp,
  and rendered on the UI dispatcher. Unrelated conversation messages are
  ignored.
- The subscription is removed when `GamePage` unloads. Guest sessions neither
  load the history nor subscribe to updates, and their chat list remains hidden.
- The server adapter must retrieve the authoritative ordered history and push
  newly persisted messages only to currently authorized participants.

## Available matches

CU-13 starts from Join a match in the main menu. The joinable list and the in-progress spectator list are separate sections so CU-12 records are never presented as available matches.

- A match is joinable only when its state is Waiting, it has fewer than two participants, it does not use an opponent controlled by the system, and its creator is another player. `AvailableMatchesService.FilterJoinable` applies all four conditions before the UI receives the list.
- Each available row shows the host, configured time per turn, waiting state, and the action that starts CU-15. Selection asks for confirmation before the client revalidates and joins the chosen match.
- Refresh repeats the complete retrieval and filtering operation. Back returns to the authenticated or guest main menu. When no joinable matches exist, the page keeps Refresh and Back available and shows the localized empty-list message.
- Connection and retrieval errors clear the joinable list and show separate localized alerts. The prototype provider currently returns deterministic sample data; the future server adapter must supply current records and the corresponding failure responses.
- The in-progress section remains available to authenticated players for CU-12 spectating and is hidden as an action for guests. It does not affect the CU-13 joinable-match filters.

## Match settings before play

CU-14 starts from the creator's waiting screen and edits only the time available per turn.

- The dialog displays the current value and applies the same `TurnTimePolicy` used by match creation: exactly one ASCII whole number from 10 through 120 seconds. Empty values, decimals, signs, letters, and out-of-range values are rejected before a save request.
- The save boundary checks the match state and creator again. A change is accepted only while the match remains in `Waiting` and the requester is still its creator. Production authorization must compare immutable account and match identifiers rather than display names.
- A successful change replaces only `TurnTimeSeconds`. The match code, participants, state, board object, and initial piece distribution remain unchanged, and the waiting page immediately shows the new value.
- Canceling, an invalid value, a match that already started, an unauthorized requester, a connection failure, or a persistence failure preserves the previous duration. The connection and persistence responses are reserved for the future match server adapter.
- `MatchSettingsService` currently updates the temporary in-memory `MatchSession` so the GUI flow can be exercised. The server must perform the state, ownership, validation, and persistence checks atomically before returning success.

## Joining a match

CU-15 starts from an available-match row or from the match-code dialog. Both paths converge on the same state, participant, and ownership checks.

- Selecting a row asks for confirmation. Canceling leaves the match unchanged. After confirmation, the service retrieves the current record again and accepts it only when it remains in `Waiting`, has exactly one participant, does not use the system opponent, and belongs to another player.
- Code entry follows `MatchCodePolicy`. A valid code must identify a currently joinable match. Invalid and unavailable codes show different localized messages and reopen the input dialog; canceling it performs no operation.
- A successful join creates the standard initial board, registers the joining player as the second participant with light pieces, keeps the creator on dark pieces with the first turn, changes the match to `InProgress`, and prevents any subsequent join through the same code.
- The active-game session records which participant is local. Chat messages, abandonment victory, instruction color, and optional profile-picture pieces therefore use the joining player and the light-piece role correctly. A guest keeps `GuestName`, cannot send chat messages, returns to the guest menu, and retains the other guest restrictions outside the game.
- Failed availability, ownership, format, or code checks create no session and do not change the match or code. The temporary catalog performs validation, session construction, and state replacement under one lock so its prototype update is not partial.
- `PrototypeMatchStore` supplies deterministic local data for exercising the interface. The server must repeat all checks atomically with immutable player and match identifiers, register exactly one second participant, establish the initial turn, invalidate further code use, and distinguish connection from persistence failures.

## Reversi moves and turn timing

CU-16 operates only while a `MatchSession` is `InProgress`. The creator has the dark pieces and the first turn; the second participant has the light pieces.

- `ReversiBoard.GetValidMoves` evaluates all eight horizontal, vertical, and diagonal directions. A position is valid only when it is empty and brackets one or more contiguous opponent pieces between the new piece and another piece of the active color. The game board marks every valid local position with a visible gold indicator.
- `TryApplyMove` builds a new board and returns it only after every captured line is known. An occupied, out-of-range, or noncapturing position returns failure and preserves the original board. The active turn and timer continue after an invalid selection.
- A confirmed move records its sequential number, player color, coordinate, UTC time, and origin (`Manual`, `Timeout`, or `System`). The new board and movement record are committed together in the session, the scores are recalculated from the board, and a manual or system move resets that player's consecutive timeout count.
- Every prepared turn starts from the match's configured duration. The WPF countdown updates once per second. On the first or second consecutive timeout for a player, `GameMoveService` chooses one of that player's current valid positions, records the movement as automatic, applies it, and leaves the player's consecutive timeout count accumulated. A manual move by that player resets the count.
- A third consecutive timeout finishes the match for inactivity and awards victory to the opponent without creating another movement. The session records the winner, end reason, and UTC completion time and rejects subsequent moves.
- When the next player has no valid moves but the player who just moved does, the unavailable turn is skipped and the same color plays again with a new timer. If the active player begins without a move, the turn passes to the opponent. If neither color has a move, the match ends and the winner is selected by piece count; equal counts produce a draw.
- `GamePage` highlights positions only for the local participant's active turn. It displays the current player, remaining time, scores, skipped turns, and automatic timeout moves, and shows the final result before returning to the appropriate menu.
- When the temporary opponent is controlled by the system, it selects a random legal move after a short visual delay. This movement is recorded with `System` origin and resets that side's timeout count; it is separate from a movement forced by an expired timer.
- The current service commits movements and results to the in-memory session so the complete interaction can be exercised. The future match server must validate the participant and turn, use an authoritative timer, persist each movement and resulting board atomically, broadcast the confirmed state, and prevent new moves while a failed final result is unresolved.

## Match abandonment

CU-17 is available only to a participant in a match whose state is `InProgress`.

- Selecting Leave match opens the reusable confirmation dialog with explicit Leave match and Continue playing actions. Canceling closes the dialog without calling the abandonment service or changing the match.
- After confirmation, `AbandonMatchService` checks the current state again. A successful operation records the abandoning participant's color and name, awards the win to the other color, sets the end reason to `Abandonment`, records the UTC completion time, changes the state to `Finished`, and invalidates the temporary match code.
- The existing board and movement collection are retained exactly as they were before abandonment. No extra movement is created.
- If the match already finished while the confirmation was open, the operation does not overwrite its winner, reason, completion time, or abandonment fields. `GamePage` displays the existing result.
- Success uses the shared match-result dialog and includes the winner's name before returning to the appropriate authenticated or guest menu. A connection or persistence failure uses its own localized alert and does not record a partial abandonment result.
- The current service applies these rules to the in-memory session. The server adapter must repeat the state check and persist the abandoning participant, opponent victory, final state, end reason, and retained movement history atomically.

## Use-case and localization status

Resource keys live in `Properties/Resources.resx` and `Properties/Resources.es-MX.resx`. Dialog titles, explanatory text, field labels, and button labels must have keys in both files. Native `MessageBox` is used for informational, warning, and error alerts; the reusable text dialog collects one value, and the password-reset dialog collects two password values.

| Flow | Client behavior now | Pending server behavior or response |
| --- | --- | --- |
| CU-01 Create account | Required fields, username, email, password, confirmation, picture, six-digit input, success dialog, and menu navigation | Username/email uniqueness, blocked-password check, code delivery and verification, persistence, session establishment, and related error alerts |
| Sign in | Required email/password, email syntax, six-digit input, and menu navigation | Credential check, code delivery and verification, session establishment |
| Reset password | Email syntax, six-digit input, password policy and match, and success dialog | Account lookup, code delivery and verification, password update, and server errors |
| CU-03 Show account information | Profile page, known session preview values, image validation, and navigation | Account lookup, authorization, statistics, stored picture, and retrieval errors |
| CU-04 Modify username | Required/format validation, cancellation, session preview update, and success dialog | Current account retrieval, uniqueness, atomic save, and occupied/connection/persistence responses |
| CU-05 Modify password | Three password fields, required/match/policy validation, cancellation, and a service-response handler | Current-password verification, blocked-password check, secure replacement, and success response; until connected, a valid submission ends with the service-unavailable alert |
| CU-06 Save profile picture | Shared file picker, PNG/JPEG validation, confirmation, cancellation, registration preview, and profile save-result handling | Upload with account creation or profile modification, secure storage, replacement of the previous picture, and a success response |
| CU-07 Show player statistics | Reusable profile section, target player identifier, totals and win-rate calculation, zero-result handling, and error alerts | Retrieval of the selected player's finished matches and public-profile navigation |
| CU-08 Save board background | Options page, session appearance switches, image selection and validation, confirmation, preview area, cancellation, and save-result handling | Account preference storage, preference loading, and application of the settings in the game board renderer |
| CU-09 Modify interface language | English/Spanish selectors on current pages, no-op for the active language, confirmation, resource availability check, and live text refresh | Account preference storage and retrieval, including persistence errors |
| CU-10 Deactivate account | Own-profile button, confirmation, result-specific alerts, and sign-out/navigation gated on success | Active-match check, soft deactivation, historical data retention, and server response |
| CU-11 Create match | Turn-time validation, temporary unique code, waiting/cancel flow, system-opponent path, standard initial board, and game interface with local appearance preferences | Atomic match persistence, remote opponent arrival, authoritative code ownership, participants, turns, and real-time chat |
| CU-12 Show match information | Previous-match list, spectator view, replay controls, participants, state, time, board frames, piece counts, movements, result handling, close navigation, and localized error branches | Authorized match/history retrieval, missing-match response, live spectator updates, and authoritative recorded movements |
| CU-13 Show available matches | Waiting/full/system filtering, available and empty states, refresh, back navigation, separate spectator section, and localized retrieval failures | Current server listing and error responses |
| CU-14 Modify match | Current-time dialog, state/creator/range checks, cancellation, success feedback, and in-memory waiting-session update | Atomic authorization and persistence using immutable server identifiers, plus connection and save errors |
| CU-15 Join match | Row confirmation, six-character code flow, current-state and ownership checks, second-participant session, guest identity, light-piece role, initial board, and code invalidation | Atomic server lookup, authorization, participant registration, state transition, and connection/persistence responses |
| CU-16 Make move | Valid-position highlighting, eight-direction capture, immutable failed attempts, movement records, score and turn updates, configured countdown, timeout moves, passes, inactivity loss, board result, and final-result dialog | Authoritative timing, participant and turn validation, atomic movement/result persistence, and real-time state broadcast |
| CU-17 Abandon match | Confirmation and cancellation, current-state recheck, abandoning participant record, opponent victory, final state and reason, retained board and movements, existing-result handling, and result dialog | Atomic server persistence and connection/persistence responses |
| CU-18 Show match history | Participant filtering, newest-first ordering, opponent, localized date, state, result, score, end reason, empty state, CU-12 navigation, and localized failure branches | Authenticated server query and connection/retrieval responses |
| CU-19 Show movement information | Match-scoped query, sequence ordering, player, position, localized timestamp, affected-piece count, manual/automatic origin, empty dialog, close action, and localized failure branches | Authorized server query and connection/retrieval responses |
| CU-20 Consult instructions | Main-menu and in-game actions, active-language loading, shared complete rules, match color/time context, modal return to origin, and loading-error handling | None |
| CU-21 Send friendship request | Player-search page, active-account filtering, circular pictures, public-profile entry point, self/friend/pending/availability checks, atomic pending request, success feedback, return navigation, and localized failure branches | Authenticated server search, relationship checks, persistence, and connection responses |
| CU-22 Show friendship requests | Authenticated menu navigation, recipient and Pending filtering, newest-first order, localized received date, circular pictures, selectable rows, empty state, return navigation, and localized failure branches | Authenticated server retrieval and connection responses |
| CU-23 Respond to friendship request | Three-action request dialog, cancellation, pending and recipient revalidation, atomic accept/reject state change, friendship creation on acceptance, list refresh, and localized result branches | Authenticated transactional persistence and connection responses |
| CU-24 Show friends | Authenticated main-menu list, accepted and active relationship filtering, stable ordering, circular pictures, empty state, public-profile navigation, and localized failure branches | Authenticated server retrieval, stored profile pictures, and connection responses |
| CU-25 Remove friend | Per-friend destructive action, named confirmation, cancellation, membership-safe relationship removal, success and missing-state refresh, preserved historical data boundary, and localized failure branches | Authenticated transactional deactivation and connection responses |
| CU-26 Send message | Trimmed 1–200 character validation, participant and guest authorization, bilingual whole-expression moderation, infringement tracking, third-offense mute, atomic session storage, success-only display and clearing, and localized failure branches | Authoritative authorization, account-wide moderation persistence, message persistence, and real-time broadcast |
| CU-27 Read messages | Participant and guest authorization, conversation-scoped chronological history, localized empty state, no partial history on failure, deduplicated live updates, unrelated-conversation filtering, and subscription cleanup | Authoritative history retrieval, connection responses, and real-time delivery to authorized participants |
| CU-28 Enter as guest | Restriction disclosure, cancel flow, shared name validation, localized visual prefix, language-independent identity, case-insensitive active-name uniqueness, temporary session lifecycle, restricted menu, game identity propagation, and localized failure branches | Server-wide temporary-session registration, uniqueness, availability, and disconnection cleanup |

### Alert key inventory

Each alert has a localized title and message. Password-policy alerts share a title and select a specific message key for the failed rule. This inventory distinguishes visible client branches from messages reserved for server responses.

| Flow | Resource prefix or name | Current trigger |
| --- | --- | --- |
| CU-01 missing fields | `AccountCreation_MissingRequiredInformation` | Client |
| CU-01 username format | `AccountCreation_InvalidUsernameFormat` | Client |
| CU-01 email format | `AccountCreation_InvalidEmailFormat`, with `InvalidEmailNameMessage`, `InvalidEmailDomainMessage`, and `EmailTooLongMessage` | Client |
| CU-01 password mismatch and format | `AccountCreation_PasswordMismatch`, `InvalidPasswordFormat`, and its specific password messages | Client |
| CU-01 unsupported picture | `AccountCreation_ImageNotSupported` | Client |
| CU-01 invalid/expired code | `AccountCreation_InvalidConfirmationCode`, `ExpiredConfirmationCode` | Server pending |
| CU-01 occupied username/email | `AccountCreation_UsernameAlreadyTaken`, `EmailAlreadyTaken` | Server pending |
| CU-01 blocked password | `AccountCreation_PasswordBlocked` | Server pending |
| CU-01 connection, email delivery, or save error | `AccountCreation_ServerConnectionFailed`, `CannotSendEmail`, `PersistenceError` | Server pending |
| CU-04 missing or invalid username | `UsernameModification_Missing`, `InvalidFormat` | Client |
| CU-04 occupied username | `UsernameModification_AlreadyTaken` | Server pending |
| CU-04 connection or save error | `UsernameModification_ServerConnectionFailed`, `PersistenceError` | Server pending |
| CU-04 successful change | `UsernameModification_Success` | Client preview only |
| CU-05 missing fields, mismatch, or invalid new password | `PasswordModification_MissingRequiredInformation`, `PasswordMismatch`, `InvalidPasswordTitle` and its specific password messages | Client |
| CU-05 incorrect current or blocked new password | `PasswordModification_CurrentPasswordIncorrect`, `BlockedPassword` | Server pending |
| CU-05 connection or save error | `PasswordModification_ServerConnectionFailed`, `PersistenceError` | Service-unavailable response now; persistence response pending |
| CU-05 successful change | `PasswordModification_Success` | Only after a future server success response |
| CU-06 invalid image | `AccountCreation_ImageNotSupported` or `AccountInformation_ImageNotSupported` | Client |
| CU-06 confirmation | `ProfileImageModification_Confirmation` | Client |
| CU-06 connection or save error | `ProfileImageModification_ServerConnectionFailed`, `PersistenceError` | Service-unavailable response now; persistence response pending |
| CU-06 successful profile change | `ProfileImageModification_Success` | Only after a future server success response |
| CU-07 field labels and consult action | `PlayerStatistics_Title`, `ShowButton`, `WinsLabel`, `MatchesPlayedLabel`, `LossesLabel`, `DrawsLabel`, `WinRateLabel` | Client |
| CU-07 connection or retrieval error | `PlayerStatistics_ServerConnectionFailed`, `RetrievalError` | Service-unavailable response now; retrieval response pending |
| Options navigation and controls | `MainMenu_SettingsButton`, `Settings_*` | Client |
| CU-08 invalid image or confirmation | `BoardBackground_ImageNotSupported`, `Confirmation` | Client |
| CU-08 connection or save error | `BoardBackground_ServerConnectionFailed`, `PersistenceError` | Service-unavailable response now; persistence response pending |
| CU-08 successful save | `BoardBackground_Success` | Only after a future server success response |
| CU-09 confirmation | `Common_ChangeLanguageTitle`, `Common_ChangeLanguageMessage` | Client |
| CU-09 unavailable language or missing resources | `LanguageSettings_NotAvailable`, `ResourcesError` | Client |
| CU-09 preference save error | `LanguageSettings_PersistenceError` | Server pending |
| CU-10 confirmation or active match | `AccountDeactivation_Confirmation`, `ActiveMatch` | Client confirmation; active-match response pending |
| CU-10 connection or save error | `AccountDeactivation_ServerConnectionFailed`, `PersistenceError` | Service-unavailable response now; persistence response pending |
| CU-10 success | `AccountDeactivation_Success` | Only after a future server success response |
| CU-11 invalid time | `MatchCreation_InvalidTurnTime` | Client |
| CU-11 connection or creation error | `MatchCreation_ServerConnectionFailed`, `PersistenceError` | Server adapter pending |
| CU-11 waiting and game interface | `WaitingForOpponent_*`, `Game_*` | Client prototype |
| CU-17 confirmation and result | `Game_LeaveConfirmation*`, `Game_ContinueMatchButton`, `Game_MatchResultTitle`, `Game_VictoryMessage` | Client prototype; authoritative result pending server |
| CU-12 history, spectator, and replay text | `MatchHistory_*`, `MatchInformation_*`, `AvailableMatches_SpectateButton` | Client prototype |
| CU-12 missing match, connection, or retrieval error | `MatchInformation_NotFound`, `ServerConnectionFailed`, `RetrievalError` | Server adapter pending |
| CU-13 list controls and states | `AvailableMatches_RefreshButton`, `WaitingState`, `TurnTimeFormat`, `EmptyMessage` | Client prototype |
| CU-13 connection or retrieval error | `AvailableMatches_ServerConnectionFailed`, `RetrievalError` | Server adapter pending |
| CU-14 successful modification | `MatchSettings_Success` | Client prototype |
| CU-14 already started or unauthorized | `MatchSettings_AlreadyStarted`, `Unauthorized` | Client boundary now; authoritative server response pending |
| CU-14 invalid time | `MatchSettings_InvalidTurnTime` | Client |
| CU-14 connection or save error | `MatchSettings_ServerConnectionFailed`, `PersistenceError` | Server adapter pending |
| CU-15 confirmation | `JoinMatch_Confirmation` | Client |
| CU-15 match unavailable or own match | `JoinMatch_NoLongerAvailable`, `CannotJoinOwnMatch` | Client prototype; authoritative server response pending |
| CU-15 invalid or unavailable code | `JoinMatch_InvalidMatchCode`, `MatchCodeNotAvailable` | Client prototype; authoritative lookup pending |
| CU-15 connection or registration error | `JoinMatch_ServerConnectionFailed`, `PersistenceError` | Server adapter pending |
| CU-16 invalid selection and turn status | `Game_InvalidPositionMessage`, `TurnSkippedMessage`, `AutomaticMoveMessage` | Client prototype |
| CU-16 final result | `Game_MatchResultTitle`, `DrawResultMessage`, `InactivityResultMessage`, `VictoryMessage` | Client prototype; authoritative persistence pending |
| CU-16 connection or movement save error | `Game_ServerConnectionFailed`, `MovementPersistenceError` | Server adapter pending |
| CU-16 final-result save error | `Game_MatchResultPersistenceError` | Server adapter pending |
| CU-17 connection or finalization error | `AbandonMatch_ServerConnectionFailed`, `PersistenceError` | Server adapter pending |
| CU-18 list, states, results, and empty response | `MatchHistory_*` | Client prototype |
| CU-18 connection or retrieval error | `MatchHistory_ServerConnectionFailed`, `RetrievalError` | Server adapter pending |
| CU-19 dialog, fields, origin, and empty response | `MovementHistory_*` | Client prototype |
| CU-19 connection or retrieval error | `MovementHistory_ServerConnectionFailed`, `RetrievalError` | Server adapter pending |
| CU-20 instructions and game context | `Instructions_Title`, `RulesMessage`, `MatchContextFormat`, `MainMenu_InstructionsButton` | Client |
| CU-20 resource loading error | `Instructions_LoadingError` | Client |
| CU-21 search and navigation | `AddFriends_*`, `AccountInformation_AddFriendButton`, `BackToFriendsButton` | Client prototype |
| CU-21 success and alternate flows | `FriendRequest_Sent`, `CannotAddYourself`, `FriendshipAlreadyExists`, `AlreadyPending`, `AccountNotAvailable` | Client prototype; authoritative server checks pending |
| CU-21 connection, search, or save error | `FriendRequest_ServerConnectionFailed`, `PersistenceError`, `AddFriends_RetrievalError` | Server adapter pending |
| CU-22 list, navigation, and empty response | `MainMenu_FriendRequests*`, `FriendRequests_Title`, `Description`, `DateFormat`, `EmptyMessage`, `BackButton` | Client prototype |
| CU-22 connection or retrieval error | `FriendRequests_ServerConnectionFailed`, `RetrievalError` | Server adapter pending |
| CU-23 response dialog and actions | `FriendRequestResponse_Title`, `Message`, `AcceptButton`, `RejectButton` | Client prototype |
| CU-23 accepted, rejected, already processed, or unauthorized | `FriendRequestResponse_Accepted`, `Rejected`, `AlreadyProcessed`, `Unauthorized` | Client prototype; authoritative server checks pending |
| CU-23 connection or update error | `FriendRequestResponse_ServerConnectionFailed`, `PersistenceError` | Server adapter pending |
| CU-24 friend rows and empty response | `MainMenu_FriendsTitle`, `FriendsEmptyMessage`, `ViewFriendProfileButton` | Client prototype |
| CU-24 connection or retrieval error | `Friends_ServerConnectionFailed`, `RetrievalError` | Server adapter pending |
| CU-25 confirmation, success, or missing relationship | `FriendRemoval_Confirmation`, `Success`, `NotFound`, `MainMenu_RemoveFriendButton` | Client prototype |
| CU-25 connection or removal error | `FriendRemoval_ServerConnectionFailed`, `PersistenceError` | Server adapter pending |
| CU-26 empty, oversized, unauthorized, muted, or blocked message | `Chat_EmptyMessage`, `MessageTooLong`, `Unauthorized`, `PlayerMuted`, `MessageBlocked` | Client prototype |
| CU-26 connection or storage error | `Chat_ServerConnectionFailed`, `MessagePersistenceError`, `ModerationPersistenceError`, `MutePersistenceError` | Server adapter pending |
| CU-27 empty history or unauthorized read | `Chat_NoMessagesMessage`, `ChatRead_Unauthorized` | Client prototype |
| CU-27 connection or retrieval error | `ChatRead_ServerConnectionFailed`, `RetrievalError` | Server adapter pending |
| CU-28 access dialog, displayed prefix, and restrictions | `LoginScreen_GuestDialog*`, `GuestAccess_DisplayNameFormat`, `MainMenu_GuestLabel` | Client prototype |
| CU-28 missing, invalid, or occupied name | `GuestAccess_MissingName`, `InvalidName`, `NameAlreadyInUse` | Client prototype; global uniqueness pending server |
| CU-28 temporary-session creation error | `GuestAccess_SessionCreationError` | Client boundary now; authoritative server response pending |

When adding a use case, record each new field's rule and boundary here, add missing text to both resource dictionaries, and connect its alternate and exception flows where the required service exists. If the service does not exist, keep the resource keys and mark the behavior as pending instead of treating a client preview as persisted data.

## Automated checks

Build `Tests/ValidationPolicyTests.csproj` and run `Tests/bin/Debug/ValidationPolicyTests.exe` to check account-input boundaries, statistics, match discovery and settings, code joining, CU-16 through CU-19 game and history rules, CU-20 instruction loading failures, CU-21 through CU-25 friendship flows, CU-26 moderation and storage, CU-27 authorization and message ordering, and CU-28 name, prefix, uniqueness, and session-lifecycle rules. The test project compiles the same policy and service source files as the WPF application and requires no external test package.
