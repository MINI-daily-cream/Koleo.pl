﻿using Domain;
using Koleo.Models;
using KoleoPL.Services;
using System.Reflection.Metadata;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Document = iTextSharp.text.Document;
using Org.BouncyCastle.Asn1.X509.SigI;
using API.Services.Interfaces;

namespace Koleo.Services
{
    public class TicketService : ITicketServive
    {
        private readonly IDatabaseServiceAPI _databaseService;
        private readonly IPaymentService _paymentService;

        public TicketService(IDatabaseServiceAPI databaseService, IPaymentService paymentService)
        {
            _databaseService = databaseService;
            _paymentService = paymentService;
        }
        public async Task<bool> Buy(string userId, List<Connection> connections, string targetName, string targetSurname)
        {
            if (await _paymentService.ProceedPayment())
            {
                var tmpResult = await Add(userId, connections, targetName, targetSurname);
                return tmpResult;
            }
            return false;
        }

        public async Task<(List<ConnectionInfoObject>, bool)> ListByUser(string userId)
        {
            var result = await GetTicketsByUser(userId);
            if (!result.Item2) return (new List<ConnectionInfoObject> { }, false);
            List<string> ticketIds = result.Item1;
            List<ConnectionInfoObject> connectionsInfo = new List<ConnectionInfoObject>();
            foreach (string ticketId in ticketIds)
            {
                var tmpResult = await UpdateConnectionsInfoList(ticketId, connectionsInfo);
                if (!tmpResult) return (new List<ConnectionInfoObject> { }, false);
            }
            return (connectionsInfo, true);
        }

        public async Task<bool> Generate(string userId, string ticketId)
        {
            List<ConnectionInfoObject> connectionsInfo = new List<ConnectionInfoObject>();
            var tmpResult = await UpdateConnectionsInfoList(ticketId, connectionsInfo);
            if (!tmpResult) return false;
            string sql = $"SELECT Target_Name, Target_Surname FROM Tickets WHERE Id = '{ticketId}'";
            var resultMain = await _databaseService.ExecuteSQL(sql);
            if (!resultMain.Item2) return false;
            var result = resultMain.Item1;
            string[] userData = result[0];
            string name = userData[0];
            string surname = userData[1];
            try
            {
                string filePath = $"ticket_{ticketId}.pdf";
                using (iTextSharp.text.Document document = new Document())
                {
                    using (PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create)))
                    {
                        document.Open();
                        Paragraph title = new Paragraph("Ticket Information");
                        title.Alignment = Element.ALIGN_CENTER;
                        document.Add(title);
                        document.Add(Chunk.NEWLINE);
                        document.Add(new Paragraph($"Name and surname:{name} {surname}"));
                        foreach (var connection in connectionsInfo)
                        {
                            document.Add(new Paragraph($"Date: {connection.Date.ToShortDateString()}"));
                            document.Add(new Paragraph($"Train Number: {connection.TrainNumber}"));
                            document.Add(new Paragraph($"Start Station: {connection.StartStation}"));
                            document.Add(new Paragraph($"End Station: {connection.EndStation}"));
                            document.Add(new Paragraph($"Provider: {connection.ProviderName}"));
                            document.Add(new Paragraph($"Source City: {connection.SourceCity}"));
                            document.Add(new Paragraph($"Destination City: {connection.DestinationCity}"));
                            document.Add(new Paragraph($"Departure Time: {connection.DepartureTime}"));
                            document.Add(new Paragraph($"Arrival Time: {connection.ArrivalTime}"));
                            document.Add(new Paragraph($"Km Number: {connection.KmNumber}"));
                            document.Add(new Paragraph($"Duration: {connection.Duration}"));
                            document.Add(Chunk.NEWLINE);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> Remove(string ticketId)
        {
            if (await _paymentService.CancelPayment())
            {
                string deleteConnectionsSql = $"DELETE FROM TicketConnections WHERE Ticket_Id = '{ticketId}'";
                var tmpResult = await _databaseService.ExecuteSQL(deleteConnectionsSql);
                if (!tmpResult.Item2) return false;
                string deleteTicketSql = $"DELETE FROM Tickets WHERE Id = '{ticketId}'";
                tmpResult = await _databaseService.ExecuteSQL(deleteTicketSql);
                return tmpResult.Item2;
            }
            return false;
        }

        public async Task<bool> Add(string userId, List<Connection> connections, string targetName, string targetSurname)
        {
            string insertTicketSql = $"INSERT INTO Tickets (User_Id, Target_Name, Target_Surname) VALUES ('{userId}', '{targetName}', '{targetSurname}')";
            var result = await _databaseService.ExecuteSQL(insertTicketSql);
            if (!result.Item2) return false;

            string getLastInsertedTicketIdSql = "SELECT last_insert_rowid()";
            var ticketIdResult = await _databaseService.ExecuteSQL(getLastInsertedTicketIdSql);
            if (!ticketIdResult.Item2) return false;
            if (ticketIdResult.Item1 != null && ticketIdResult.Item1.Count > 0)
            {
                string ticketId = new string(ticketIdResult.Item1[0][0]);

                foreach (var connection in connections)
                {
                    string insertTicketConnectionSql = $"INSERT INTO TicketConnections (Ticket_Id, Connection_Id) VALUES ('{ticketId}', '{connection.Id}')";
                    var tmpresult = await _databaseService.ExecuteSQL(insertTicketConnectionSql);
                    if (!tmpresult.Item2) return false;
                }
            }
            return true;
        }

        public async Task<bool> ChangeDetails(string userId, string ticketId, string newTargetName, string newTargetSurname)
        {
            string updateDetailsSql = $"UPDATE Tickets SET Target_Name = '{newTargetName}', Target_Surname = '{newTargetSurname}' WHERE Id = '{ticketId}'";
            var tmpResult = await _databaseService.ExecuteSQL(updateDetailsSql);
            if(!tmpResult.Item2) return false;
            var result = await Generate(userId, ticketId);
            return result;
        }

        private async Task<(List<string>?, bool)> GetTicketsByUser(string userId)
        {
            string sql = $"SELECT Id FROM Tickets WHERE User_Id = {userId}";
            var result = await _databaseService.ExecuteSQL(sql);
            if (!result.Item2) return (null, false);
            return (result.Item1.Select(row => row[0]).ToList(), true);
            //return (result.Item1.Select(row => string.Parse(row[0])).ToList(), true);
        }

        private async Task<(List<Connection>?, bool)> GetConnectionsByTicket(string ticketId)
        {
            string sql = $"SELECT c.* FROM TicketConnections tc JOIN Connections c ON tc.Connection_Id = c.Id WHERE tc.Ticket_Id = '{ticketId}'";
            var result = await _databaseService.ExecuteSQL(sql);
            if(!result.Item2) return(null, false);
            return (result.Item1.Select(row => new Connection
            {
                Id = Guid.Parse(row[0]),
                //Id = row[0],
                StartStation_Id = row[1],
                EndStation_Id = row[2],
                Train_Id = row[3],
                StartTime = DateTime.Parse(row[4]),
                EndTime = DateTime.Parse(row[5]),
                KmNumber = int.Parse(row[6]),
                Duration = TimeSpan.Parse(row[7])
            }).ToList(), true);
        }

        private async Task<(string?, bool)> GetStationNameById(string stationId)
        {
            string sql = $"SELECT Name FROM Stations WHERE Id = {stationId}";
            var result = await _databaseService.ExecuteSQL(sql);
            if (!result.Item2) return (null, false);
            return (result.Item1[0][0], true);
        }

        private async Task<(string?, bool)> GetProviderNameById(string trainId)
        {
            string sql = $"SELECT Name FROM Providers WHERE Id = {trainId}";
            var result = await _databaseService.ExecuteSQL(sql);
            if (!result.Item2) return (null, false);
            return (result.Item1[0][0], true);
        }

        private async Task<(string?, bool)> GetCityNameByStationId(string stationId)
        {
            string sql = $"SELECT c.Name FROM Cities c JOIN CityStations cs ON c.Id = cs.City_Id WHERE cs.Station_Id = {stationId}";
            var result = await _databaseService.ExecuteSQL(sql);
            if (!result.Item2) return (null, false);
            return (result.Item1[0][0], true);
        }

        private async Task<bool> UpdateConnectionsInfoList(string ticketId, List<ConnectionInfoObject> connectionsInfo)
        {
            var result = await GetConnectionsByTicket(ticketId);
            if(!result.Item2) return false;
            List<Connection> connections = result.Item1;
            foreach (Connection connection in connections)
            {

                var startStationName = await GetStationNameById(connection.StartStation_Id);
                var endStationName = await GetStationNameById(connection.EndStation_Id);
                var providerName = await GetProviderNameById(connection.Train_Id);
                var sourceCityName = await GetCityNameByStationId(connection.StartStation_Id);
                var destinationCityName = await GetCityNameByStationId(connection.EndStation_Id);
                if (!startStationName.Item2 || !endStationName.Item2 || !providerName.Item2 || !sourceCityName.Item2 || !destinationCityName.Item2) return false;
                connectionsInfo.Add(new ConnectionInfoObject
                {
                    Date = connection.StartTime.Date,
                    TrainNumber = connection.Train_Id,
                    StartStation = startStationName.Item1,
                    EndStation = endStationName.Item1,
                    ProviderName = providerName.Item1,
                    SourceCity = sourceCityName.Item1,
                    DestinationCity = destinationCityName.Item1,
                    DepartureTime = connection.StartTime.ToShortTimeString(),
                    ArrivalTime = connection.EndTime.ToShortTimeString(),
                    KmNumber = connection.KmNumber,
                    Duration = connection.Duration
                });
            }
            return true;
        }
    }
}
