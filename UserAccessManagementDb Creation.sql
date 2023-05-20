Create Database UserAccessManagement
Go

Use UserAccessManagement
Go

Create Table AccessRoles
(
	Id UniqueIdentifier Primary Key Nonclustered,
	[Description] Varchar(100)
)

Create Table UserAccesses
(
	Id UniqueIdentifier Primary Key Nonclustered,
	Email Varchar(250),
	[Role] UniqueIdentifier,
	Salt Binary(16),
	[Hash] Binary(36)
)

Create Unique Clustered Index Idx_UserAccess_Email
On UserAccesses (Email);

Create Table UserInvitations
(
	Id UniqueIdentifier Primary Key Nonclustered,
	InvitingUser UniqueIdentifier,
	InvitationCode Char(50),
	Email Varchar(250),
	[Role] UniqueIdentifier,
	DateIssued DateTime,
	Expiry DateTime,
	Consumed Bit
)

Insert Into AccessRoles(Id, [Description]) 
Values
(NewId(), 'User Administrator'),
(NewId(), 'Resource Consumer')


--Create Job that will delete expired Refresh Tokens
Create Table CurrentRefreshTokens
(
	Id UniqueIdentifier Primary Key Nonclustered,
	[User] UniqueIdentifier,
	Token Char(250),
	Issued Datetime,
	Expiry Datetime
)

Create Nonclustered Index Idx_RefreshTokens_CoversAll
ON CurrentRefreshTokens ([User], Token)
Include (Id,Issued,Expiry);

Create Procedure DeleteExpiredRefreshTokens
As
Begin
	Delete CRT 
	From CurrentRefreshTokens CRT
	Where CRT.Expiry <= GetDate() 
End