## TalkWave Messaging API
![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white) ![AWS](https://img.shields.io/badge/AWS-%23FF9900.svg?style=for-the-badge&logo=amazon-aws&logoColor=white) ![Postgres](https://img.shields.io/badge/postgres-%23316192.svg?style=for-the-badge&logo=postgresql&logoColor=white) ![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white) ![Redis](https://img.shields.io/badge/redis-%23DD0031.svg?style=for-the-badge&logo=redis&logoColor=white) ![GitHub Actions](https://img.shields.io/badge/github%20actions-%232671E5.svg?style=for-the-badge&logo=githubactions&logoColor=white)

[TalkWave](https://talkwaveapp.com/) is a messaging app that allows users to chat with their friends or in groups. This repo hosts the backend code.

- [Frontend React](https://github.com/Gabefire/TalkWave)

>[!IMPORTANT]
> A majority of the server architecture is serverless, so there might be a start-up cost
> on the first load. 

## Dev Features
- App is fully deployed on AWS architecture and uses AWS Lambda, AWS ECS, AWS EKS, AWS EC2, AWS ElasticCache, API gateway, Route 53 and AWS RDS
- Fully test driven
- CI/CD pipeline for deployment and testing

AWS infrastructure:
![image](https://github.com/user-attachments/assets/e07276f5-cc96-4863-b97a-05b48f63b42a)

## Features
- Custom JWT based authentication system
- Group management
- Microservice architecture
- Fully managed database
- Live chat implemented using [SignalR](https://dotnet.microsoft.com/en-us/apps/aspnet/signalr)

## Planned features
- [ ] Create a separate auth controller outside the mono repo
- [ ] User profile pictures

