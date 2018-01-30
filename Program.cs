using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Net.Mail;


namespace PersonLoad

{
    public enum ExitCode : int
    {
        Success = 0,
        InvalidParameter = 1,
        FlagFileNotFound = 2,
        DataFileNotFound = 3,
        MultipleFlagFilesFound = 4,
        InvalidInputFile = 5,
        CreateLogFileError = 6,
        DuplicateWorksheet = 7,
        CreateSduFileError = 8,
        CreateFlagFileError = 9,
        InvalidRecordLength = 10,
        NoRecordsFound = 11,
        CountMismatch = 12,
        Other = 99,
        UnknownError = 999
    }

    public enum PersonStatus : int { Inactive, Active}

    public static class Record
    {
        private static string inserted = "inserted";
        private static string deleted = "deleted";
        
        public static string Inserted { get { return inserted;} }
        public static string Deleted { get { return deleted; } }
    }
    public static class ChangeType
    {
        private static string newPerson = "person added";
        private static string updatedPerson = "person updated";
        private static string mergedPerson = "person merged";
        private static string unchangedPerson = "person unchanged";
        private static string skippedPerson = "person skipped";
        private static string newCase = "case added";
        private static string updatedCase = "case updated";
        private static string skippedCase = "case skipped";
        private static string unchangedCase = "case unchanged";

        public static string NewPerson { get { return newPerson; }  }
        public static string UpdatedPerson { get { return updatedPerson; } }
        public static string MergedPerson { get { return mergedPerson;}  }
        public static string UnchangedPerson { get { return unchangedPerson; } }
        public static string SkippedPerson{ get { return skippedPerson; } }
        public static string NewCase { get { return newCase; } }
        public static string UpdatedCase { get { return updatedCase; } }
        public static string SkippedCase { get { return skippedCase; } }
        public static string UnchangedCase { get { return unchangedCase; } }
    }

    public class DataFile
    {
        public static string Directory { get; set; }
        public static string Path { get; set; }
        public static bool IsWeekly { get; set; }

    }
    class Program
    {   
        private const int recordLength = 550;
        private static int recordCount;
        private static int recordCount04;
        private static int recordCount05;
        private static int recordCount07;
        private static Log progLog;
        private static Log scriptLog;
        private static RecordType01 rec01 = new RecordType01();
        private static RecordType04 rec04 = new RecordType04();
        private static RecordType05 rec05 = new RecordType05();
        private static RecordType07 rec07 = new RecordType07();

        private static bool firstRecord = true;

        private enum DataFileType : byte { Daily, Weekly, Unknown };

        static void Main(string[] args)
        {
            try
            {
                //Program log
                progLog = new Log(ConfigurationManager.AppSettings["ProgLogDirPath"]
                                    + ConfigurationManager.AppSettings["ProgLogFileName"]
                                    + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString()
                                    + ConfigurationManager.AppSettings["ProgLogFileExt"]);
                //+ Path.GetFileName(DataFile.Path)
                //        .Replace(ConfigurationManager.AppSettings["DataFileExt"], ConfigurationManager.AppSettings["ProgLogFileExt"]));

                progLog.WriteLine(String.Format("----- Start: {0} -----", DateTime.Now.ToString()));

                DisableTriggers();                
                                
                //Get Input File
                DataFile.Directory = ConfigurationManager.AppSettings["DataFileDirPath"].ToString();

                string[] files = Directory.GetFiles(DataFile.Directory, "*.FLG");

                List<string> fileList = new List<string>();

                if (files.Count() == 1)
                    DataFile.Path = Regex.Replace(files[0].ToString(), ".FLG", ConfigurationManager.AppSettings["DataFileExt"], RegexOptions.IgnoreCase);
                else if (files.Count() == 0)
                    throw new CustomException("No flag files found");
                else
                {
                    foreach (var file in files)
                    {
                        fileList.Add(file);
                    }
                    fileList.Sort();
                    DataFile.Path = Regex.Replace(fileList[0].ToString(), ".FLG", ConfigurationManager.AppSettings["DataFileExt"], RegexOptions.IgnoreCase);
                }
                    

                progLog.WriteLine("Input File: " + DataFile.Path);
                    
                // If it is a Weekly file set All cases to be deleted
                string fileName = Path.GetFileName(DataFile.Path);

                Match weeklyFileMatch = Regex.Match(fileName, @"^WP[\w]*.(DAT)$", RegexOptions.IgnoreCase);
                DataFile.IsWeekly = weeklyFileMatch.Success;

                if (DataFile.IsWeekly)
                {
                    ///* 2017-03-30 CS
                    ///-------------------------------------
                    ///Management requested not to delete 
                    ///the cases for the weekly file
                    ///-------------------------------------
                    /*
                    using(var db = new PersonDBContext())
                    {
                        db.Database.ExecuteSqlCommand("UPDATE KCCases SET ToBeDeleted = 1");
                    } 
                     */
                }
                else
                {
                    Match dailyFileMatch = Regex.Match(fileName, @"^DP[\w]*.(DAT)$", RegexOptions.IgnoreCase);
                    if (!dailyFileMatch.Success)
                        throw new CustomException("Invalid File Name: " + DataFile.Path);
                }
                
                //Script log
                scriptLog = new Log(ConfigurationManager.AppSettings["ScriptLogDirPath"]
                                    + ConfigurationManager.AppSettings["ScriptLogFileName"]
                                    + Path.GetFileName(DataFile.Path)
                                            .Replace(ConfigurationManager.AppSettings["DataFileExt"]
                                                , ConfigurationManager.AppSettings["ScriptLogFileExt"]));

                scriptLog.WriteLine("USE " + ConfigurationManager.AppSettings["ScriptLogDatabase"] + ";");
                scriptLog.WriteLine("DECLARE @KCPersonLoadID INT;");

                // Set streams for reading input data file
                FileStream fs = new FileStream(DataFile.Path, FileMode.Open);
                StreamReader sr = new StreamReader(fs);

                char[] buffer = new char[recordLength];
                StringBuilder sb = new StringBuilder();
                
                while (sr.Peek() >= 0)
                {
                    sr.Read(buffer, 0, recordLength);

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        sb.Append(buffer[i]);
                    }

                    ProcessRecord(sb.ToString());
                    recordCount += 1;
                    sb.Clear();
                }

                if(recordCount04 == 0 && recordCount05 == 0 && recordCount07 == 0)
                {
                    //throw (new CustomException("File is invalid. No detail records found"));
                    progLog.WriteLine("WARNING! No 04, 05. 07 records found.");
                }

                EnableTriggers();

                if (recordCount == rec01.RecordCount) {                    
                    /// If it is a weekly file delete court cases
                    /// CS 12/29/2017 
                    /// Commented this out because it wasn't doing anything.
                    /// We are not deletin the cases anymore.  The section of 
                    /// code that updates the ToBeDeleted = 1 was commented out
                    /// a long time ago. So the code below was doing nothing because
                    /// ToBeDeleted was always 0 (false)

                    //if (DataFile.IsWeekly)
                    //{
                    //    using(var db = new PersonDBContext())
                    //    {
                    //        db.Database.ExecuteSqlCommand("DELETE FROM KCCases WHERE ToBeDeleted = 1"); 
                    //    }
                    //}

                    //Delete flag file
                    File.Delete(Regex.Replace(DataFile.Path, ConfigurationManager.AppSettings["DataFileExt"], ".FLG", RegexOptions.IgnoreCase));


                    //Complete log and exit
                    LogRecordCounts();
                    progLog.WriteLine(String.Format("{0}----- Done: {1} -----", Environment.NewLine, DateTime.Now.ToString()));
                    progLog.Close();
                    scriptLog.Close();
                    EmailNotification((int)ExitCode.Success);
                    System.Environment.Exit((int)ExitCode.Success);
                }
                else
                {
                    throw new CustomException(String.Format(
                                            "The Header record count of {0} does not macth the Processing record count of {1}"
                                            , rec01.RecordCount
                                            , recordCount)
                                        );                    
                }
            }
            catch(Exception ex)
            {
                if (progLog != null)
                {
                    LogRecordCounts();
                    progLog.WriteLine(ex.ToString());
                    progLog.Close();
                }

                if (scriptLog != null)
                {
                    scriptLog.Close();
                }
                
                EmailNotification((int)ExitCode.UnknownError);
                System.Environment.Exit((int)ExitCode.UnknownError);
            }
        }

        private static void EnableTriggers()
        {
            using (var db = new PersonDBContext())
            {
                db.Database.ExecuteSqlCommand("ENABLE TRIGGER dbo.tu_KCPerson on dbo.KCPerson;");
                db.Database.ExecuteSqlCommand("ENABLE TRIGGER dbo.tu_KCCases on dbo.KCCases;");
            }
        }

        private static void DisableTriggers()
        {
            using (var db = new PersonDBContext())
            {
                db.Database.ExecuteSqlCommand("DISABLE TRIGGER dbo.tu_KCPerson on dbo.KCPerson;");
                db.Database.ExecuteSqlCommand("DISABLE TRIGGER dbo.tu_KCCases on dbo.KCCases;");
            }
        }
              
        private static void ProcessRecord(string recordContent)
        {
            string recordType = recordContent.Substring(0, 2);

            if (firstRecord)
            {
                if (recordType == "01")
                {
                    rec01.RecordType = recordContent.Substring((int)RecordType01.FieldIndex.RecordType, (int)RecordType01.FieldLength.RecordType);
                    rec01.RecordCount = Int32.Parse(
                                            recordContent.Substring((int)RecordType01.FieldIndex.RecordCount
                                                                    , (int)RecordType01.FieldLength.RecordCount)
                                        );

                    //2015-07-18-17.16.06.391577
                    string strDateTime = recordContent.Substring((int)RecordType01.FieldIndex.CreationStamp, (int)RecordType01.FieldLength.CreationStamp);

                    int year = Int16.Parse(strDateTime.Substring(0, 4));
                    int month = Int16.Parse(strDateTime.Substring(5, 2));
                    int day = Int16.Parse(strDateTime.Substring(8, 2));
                    int hours = Int16.Parse(strDateTime.Substring(11, 2));
                    int minutes = Int16.Parse(strDateTime.Substring(14, 2));
                    int seconds = Int16.Parse(strDateTime.Substring(17, 2));
                    int milliseconds = Int32.Parse(strDateTime.Substring(20, 3));

                    rec01.CreationStamp = new DateTime(year, month, day, hours, minutes, seconds, milliseconds);

                    firstRecord = false;
                }
                else
                {
                    throw (new CustomException("Invalid File. First Record is not header record"));
                }
            }
            else if (recordType == "04")
            {
                try
                {
                    rec04.RecordType = recordContent.Substring((int)RecordType04.FieldIndex.RecordType, (int)RecordType04.FieldLength.RecordType);
                    rec04.PrimaryArpID = recordContent.Substring((int)RecordType04.FieldIndex.PrimaryArpID, (int)RecordType04.FieldLength.PrimaryArpID);
                    rec04.SecondaryArpID = recordContent.Substring((int)RecordType04.FieldIndex.SecondaryArpID, (int)RecordType04.FieldLength.SecondaryArpID);

                    string strDateTime = recordContent.Substring((int)RecordType04.FieldIndex.ModifiedStamp, (int)RecordType04.FieldLength.ModifiedStamp);
                    int year = Int16.Parse(strDateTime.Substring(0, 4));
                    int month = Int16.Parse(strDateTime.Substring(5, 2));
                    int day = Int16.Parse(strDateTime.Substring(8, 2));
                    int hours = Int16.Parse(strDateTime.Substring(11, 2));
                    int minutes = Int16.Parse(strDateTime.Substring(14, 2));
                    int seconds = Int16.Parse(strDateTime.Substring(17, 2));
                    int milliseconds = Int32.Parse(strDateTime.Substring(20, 3));
                    rec04.ModifiedStamp = new DateTime(year, month, day, hours, minutes, seconds, milliseconds);

                    ProcessMerge(rec04);

                    recordCount04 += 1;
                }
                catch(Exception ex)
                {
                    progLog.WriteLine("Error on Record: " + recordContent);
                    progLog.WriteLine(ex.ToString());
                    recordCount04 += 1;
                }
            }

            else if (recordType == "05")
            {
                try {
                    rec05.RecordType = recordContent.Substring((int)RecordType05.FieldIndex.RecordType, (int)RecordType05.FieldLength.RecordType);
                    rec05.PersonID = recordContent.Substring((int)RecordType05.FieldIndex.PersonID, (int)RecordType05.FieldLength.PersonID);
                    rec05.SSN_FTIN = recordContent.Substring((int)RecordType05.FieldIndex.Ssn_ftin, (int)RecordType05.FieldLength.Ssn_ftin);
                    rec05.LastName = recordContent.Substring((int)RecordType05.FieldIndex.LastName, (int)RecordType05.FieldLength.LastName);
                    rec05.FirstName = recordContent.Substring((int)RecordType05.FieldIndex.FirstName, (int)RecordType05.FieldLength.FirstName);
                    rec05.MiddleName = recordContent.Substring((int)RecordType05.FieldIndex.MiddleName, (int)RecordType05.FieldLength.MiddleName);
                    rec05.Suffix = recordContent.Substring((int)RecordType05.FieldIndex.Suffix, (int)RecordType05.FieldLength.Suffix);
                    rec05.Address1 = recordContent.Substring((int)RecordType05.FieldIndex.Address1, (int)RecordType05.FieldLength.Address1);
                    rec05.Address2 = recordContent.Substring((int)RecordType05.FieldIndex.Address2, (int)RecordType05.FieldLength.Address2);
                    rec05.Address3 = recordContent.Substring((int)RecordType05.FieldIndex.Address3, (int)RecordType05.FieldLength.Address3);
                    rec05.City = recordContent.Substring((int)RecordType05.FieldIndex.City, (int)RecordType05.FieldLength.City);
                    rec05.State = recordContent.Substring((int)RecordType05.FieldIndex.State, (int)RecordType05.FieldLength.State);
                    rec05.Zip = recordContent.Substring((int)RecordType05.FieldIndex.Zip, (int)RecordType05.FieldLength.Zip);
                    rec05.HomePhone = recordContent.Substring((int)RecordType05.FieldIndex.HomePhone, (int)RecordType05.FieldLength.HomePhone);
                    rec05.CellPhone = recordContent.Substring((int)RecordType05.FieldIndex.CellPhone, (int)RecordType05.FieldLength.CellPhone);
                    rec05.WorkPhone = recordContent.Substring((int)RecordType05.FieldIndex.WorkPhone, (int)RecordType05.FieldLength.WorkPhone);
                    rec05.WorkExtension = recordContent.Substring((int)RecordType05.FieldIndex.WorkExtension, (int)RecordType05.FieldLength.WorkExtension);
                    rec05.PagerNumber = recordContent.Substring((int)RecordType05.FieldIndex.PagerNumber, (int)RecordType05.FieldLength.PagerNumber);


                    string strDateTime = recordContent.Substring((int)RecordType05.FieldIndex.DateofBirth, (int)RecordType05.FieldLength.DateofBirth);
                    if (!String.IsNullOrEmpty(strDateTime.Trim()) && strDateTime != "00000000")
                    {
                        int year = Int16.Parse(strDateTime.Substring(0, 4));
                        int month = Int16.Parse(strDateTime.Substring(4, 2));
                        int day = Int16.Parse(strDateTime.Substring(6, 2));
                        rec05.DateofBirth = new DateTime(year, month, day);
                    }
                    else rec05.DateofBirth = null;

                    strDateTime = recordContent.Substring((int)RecordType05.FieldIndex.DateofDeath, (int)RecordType05.FieldLength.DateofDeath);
                    if (!String.IsNullOrEmpty(strDateTime.Trim()) && strDateTime != "00000000")
                    {
                        int year = Int16.Parse(strDateTime.Substring(0, 4));
                        int month = Int16.Parse(strDateTime.Substring(4, 2));
                        int day = Int16.Parse(strDateTime.Substring(6, 2));
                        rec05.DateofDeath = new DateTime(year, month, day);
                    }
                    else rec05.DateofDeath = null;

                    rec05.Country = recordContent.Substring((int)RecordType05.FieldIndex.Country, (int)RecordType05.FieldLength.Country);
                    rec05.FamilyViolenceInd = recordContent.Substring((int)RecordType05.FieldIndex.FamilyViolenceInd, (int)RecordType05.FieldLength.FamilyViolenceInd);
                    rec05.EmailAddress = recordContent.Substring((int)RecordType05.FieldIndex.EmailAddress, (int)RecordType05.FieldLength.EmailAddress);
                    rec05.NotificationEmailAddress = recordContent.Substring((int)RecordType05.FieldIndex.NotificationEmailAddress, (int)RecordType05.FieldLength.NotificationEmailAddress);
                    rec05.DeliveryMethod = recordContent.Substring((int)RecordType05.FieldIndex.DeliveryMethod, (int)RecordType05.FieldLength.DeliveryMethod);
                    rec05.IVDStatus = recordContent.Substring((int)RecordType05.FieldIndex.IvdStatus, (int)RecordType05.FieldLength.IvdStatus);
                    rec05.SDUInd = recordContent.Substring((int)RecordType05.FieldIndex.SduInd, (int)RecordType05.FieldLength.SduInd);
                    rec05.Filler = recordContent.Substring((int)RecordType05.FieldIndex.Filler, (int)RecordType05.FieldLength.Filler);

                    if (IsOrganization(rec05.PersonID))
                        ProcessOrganization(rec05);
                    else
                        ProcessPerson(rec05);

                    recordCount05 += 1;
                }
                catch(Exception ex)
                {
                    progLog.WriteLine("Error on Record: " + recordContent);
                    progLog.WriteLine(ex.ToString());
                    recordCount05 += 1;
                }
                
            }

            else if (recordType == "07")
            {
                try
                {
                    rec07.RecordType = recordContent.Substring((int)RecordType07.FieldIndex.RecordType, (int)RecordType07.FieldLength.RecordType);
                    rec07.PersonID = recordContent.Substring((int)RecordType07.FieldIndex.PersonID, (int)RecordType07.FieldLength.PersonID);
                    rec07.Court = recordContent.Substring((int)RecordType07.FieldIndex.Court, (int)RecordType07.FieldLength.Court);
                    rec07.CountyNumber = recordContent.Substring((int)RecordType07.FieldIndex.CountyNumber, (int)RecordType07.FieldLength.CountyNumber);
                    rec07.CaseType = recordContent.Substring((int)RecordType07.FieldIndex.CaseType, (int)RecordType07.FieldLength.CaseType);
                    rec07.YearCode = recordContent.Substring((int)RecordType07.FieldIndex.YearCode, (int)RecordType07.FieldLength.YearCode);
                    rec07.SeqNum = recordContent.Substring((int)RecordType07.FieldIndex.SeqNum, (int)RecordType07.FieldLength.SeqNum);
                    rec07.CaseCaption = recordContent.Substring((int)RecordType07.FieldIndex.CaseCaption, (int)RecordType07.FieldLength.CaseCaption);
                    rec07.OldCaseNumber = recordContent.Substring((int)RecordType07.FieldIndex.OldCaseNumber, (int)RecordType07.FieldLength.OldCaseNumber);
                    rec07.FIPSCode = recordContent.Substring((int)RecordType07.FieldIndex.FIPSCode, (int)RecordType07.FieldLength.FIPSCode);

                    string strDateTime = recordContent.Substring((int)RecordType07.FieldIndex.ModifiedStamp, (int)RecordType07.FieldLength.ModifiedStamp);
                    int year = Int16.Parse(strDateTime.Substring(0, 4));
                    int month = Int16.Parse(strDateTime.Substring(5, 2));
                    int day = Int16.Parse(strDateTime.Substring(8, 2));
                    int hours = Int16.Parse(strDateTime.Substring(11, 2));
                    int minutes = Int16.Parse(strDateTime.Substring(14, 2));
                    int seconds = Int16.Parse(strDateTime.Substring(17, 2));
                    int milliseconds = Int32.Parse(strDateTime.Substring(20, 3));
                    rec07.ModifiedStamp = new DateTime(year, month, day, hours, minutes, seconds, milliseconds);

                    rec07.PayorPayee = recordContent.Substring((int)RecordType07.FieldIndex.PayorPayee, (int)RecordType07.FieldLength.PayorPayee);
                    rec07.CourtCaseId = recordContent.Substring((int)RecordType07.FieldIndex.CourtCaseId, (int)RecordType07.FieldLength.CourtCaseId);
                    rec07.IVDCaseNumber = recordContent.Substring((int)RecordType07.FieldIndex.IVDCaseNumber, (int)RecordType07.FieldLength.IVDCaseNumber);
                    rec07.Filler = recordContent.Substring((int)RecordType07.FieldIndex.Filler, (int)RecordType07.FieldLength.Filler);

                    ProcessCase(rec07);

                    recordCount07 += 1;
                }
                catch (Exception ex)
                {
                    progLog.WriteLine("Error on Record: " + recordContent);
                    progLog.WriteLine(ex.ToString());
                    recordCount07 += 1;
                }
            }
            else 
            {
                throw (new CustomException("Invalid Record Type" + recordType)); 
            }
        }

        private static bool IsOrganization(string personID)
        {
            if (String.Compare(personID.Substring(0, 2), "OG", true) == 0)
                return true;
            else
                return false;
        }

        private static void ProcessMerge(RecordType04 rec04)
        {
            try
            {
                #region Comments
                /// The primary ARP is the new ARP.  The secondary ARP 
                /// is the ARP that should currently exists in our DB
                ///Check to see if the old ARP and New ARP are not empty
                ///If they are write to the log.
                ///If both have values, search for old values in Person Entity
                ///if old value doesn't exist write this to the log
                ///if new value doesn't exist writ this to the log
                ///if new value exist then the value has already been merged.
                ///Old value should exist in the PersonAlias entity
                #endregion

                if (!String.IsNullOrEmpty(rec04.PrimaryArpID.Trim()) && !String.IsNullOrEmpty(rec04.SecondaryArpID.Trim()))
                {

                    using(var db = new PersonDBContext())
                    {
                        // primary person
                        var pp = GetBestPerson(rec04.PrimaryArpID, db);

                        if(!String.IsNullOrEmpty(pp.PersonID))
                        {
                            UpdateAliasAndCleanupPersonTables(db, pp);
                        }
                        else
                        {
                            var sp = GetBestPerson(rec04.SecondaryArpID, db);
                            if (!String.IsNullOrEmpty(sp.PersonID))
                            {
                                UpdateSecondaryToBePrimary(rec04, sp, db);
                                UpdateAliasAndCleanupPersonTables(db, sp);
                            }
                            else{

                                pp = GetSecondBestPerson(rec04.PrimaryArpID, db);

                                if (pp != null)
                                {
                                    UpdateAliasAndCleanupPersonTables(db, pp);
                                }
                                else
                                {
                                    sp = GetSecondBestPerson(rec04.SecondaryArpID, db);

                                    if (sp != null)
                                    {
                                        UpdateSecondaryToBePrimary(rec04, sp, db);
                                        UpdateAliasAndCleanupPersonTables(db, sp);
                                    }
                                    else
                                    {
                                       // log not primary or secondary found;  Should never happen.
                                        progLog.WriteLine(String.Format("Primary ({0}) & secondary ({1}) ARP not found in KCPerson", rec04.PrimaryArpID, rec04.SecondaryArpID));
                                    }
                                }                                
                            }
                        }                                                                 

                        db.SaveChanges();
                    }
                }
                //else Primary and secondary arp are null
                else
                {
                    StringBuilder sb = null;

                    if (String.IsNullOrEmpty(rec04.PrimaryArpID))
                        sb.AppendLine("Primary ARP missing: " + rec04.PrimaryArpID);
                    if (String.IsNullOrEmpty(rec04.SecondaryArpID))
                        sb.AppendLine("Secondary ARP missing: " + rec04.SecondaryArpID);

                    progLog.WriteLine("04 Record skipped. Count:" + recordCount);
                    progLog.WriteLine(sb.ToString());
                }

            }
            catch
            { throw; }
                
        }

        private static void UpdateAliasAndCleanupPersonTables(PersonDBContext db, KCPerson pp)
        {
            AddSecondaryToAlias(db, pp);
            RemoveSecondaries(db);
            RemovePrimaryDups(db, pp);
        }

        private static void UpdateSecondaryToBePrimary(RecordType04 rec04, KCPerson sp, PersonDBContext db)
        {
            
            LogPersonLoad(sp.KCPersonID, ChangeType.MergedPerson);

            //Log person before changes
            LogPersonLoadDetail(Record.Deleted, sp);
            
            sp.PersonID = rec04.PrimaryArpID; //update secondary found record with primary arp;
            db.SaveChanges();
            //Log person after changes
            LogPersonLoadDetail(Record.Inserted, sp);

        }

        private static void AddSecondaryToAlias(PersonDBContext db, KCPerson person)
        {
            // ADD / UPDATE ALIAS
            var personAlias = db.KCPersonAliases.FirstOrDefault(x => x.PersonID == rec04.SecondaryArpID);

            if (personAlias != null)
            {
                personAlias.KCPersonID = person.KCPersonID;
                personAlias.SSN = person.SSN;
                personAlias.LastName = person.LastName;
                personAlias.FirstName = person.FirstName;
                personAlias.MiddleName = person.MiddleName;
                personAlias.Suffix = person.Suffix;
                //personAlias.ModifiedStamp = rec04.ModifiedStamp; //handled by update trigger
                personAlias.ModifiedBy = "ArpLoad";
                personAlias.PersonID = rec04.SecondaryArpID;
            }

            else
            {
                personAlias = new KCPersonAlias
                {
                    KCPersonAliasID = Guid.NewGuid(),
                    KCPersonID = person.KCPersonID,
                    SSN = person.SSN,
                    LastName = person.LastName,
                    FirstName = person.FirstName,
                    MiddleName = person.MiddleName,
                    Suffix = person.Suffix,
                    ModifiedStamp = rec04.ModifiedStamp,
                    ModifiedBy = "ArpLoad",
                    PersonID = rec04.SecondaryArpID,
                };
                db.KCPersonAliases.Add(personAlias);
            }
        }

        private static void RemovePrimaryDups(PersonDBContext db, KCPerson pp)
        {
            var primaryDups = from p in db.KCPersons
                              where p.PersonID == pp.PersonID
                              && p.KCPersonID != pp.KCPersonID
                              select p;

            db.KCPersons.RemoveRange(primaryDups);
            db.SaveChanges();
        }

        private static void RemoveSecondaries(PersonDBContext db)
        {
            IQueryable<KCPerson> secondaries = from p in db.KCPersons
                                               where p.PersonID == rec04.SecondaryArpID
                                               select p;

            db.KCPersons.RemoveRange(secondaries);
            db.SaveChanges();
        }

        private static KCPerson GetBestPerson(string arp, PersonDBContext db )
        {
            List<KCPerson> persons = db.KCPersons.Where(p => p.PersonID == arp)
                                                .OrderByDescending(p => p.ModifiedStamp)
                                                .ToList();

            foreach (var p in persons)
            {
                if (ValidSSN(p.SSN))
                {
                    return p;
                }
            }

            return new KCPerson();// return empty person;
        }

        private static KCPerson GetSecondBestPerson(string arp, PersonDBContext db)
        {

            KCPerson person = db.KCPersons.Where(p => p.PersonID == arp)
                                                .OrderByDescending(p => p.ModifiedStamp)
                                                .FirstOrDefault();

            return person;
        }

        private static bool ValidSSN(string ssn)
        {
            try
            {
                if (ssn != null)
                {
                    ssn = ssn.Trim().Replace("-", "");                

                    Regex regex = new Regex("^[0-9]{9}$");

                    if (regex.Match(ssn).Success)
                        return true;
                    else
                        return false;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                throw;
            }
        }

        private static void ProcessPerson(RecordType05 rec05)
        {
            try
            {
                using (var db = new PersonDBContext())
                {
                    var person = db.KCPersons.SingleOrDefault(x => x.PersonID == rec05.PersonID);
                    
                    if (person == null)
                    {
                        //new person
                        person = PersonCreate(rec05);                                          

                        db.KCPersons.Add(person);

                        LogPersonLoad(person.KCPersonID, ChangeType.NewPerson);
                        LogPersonLoadDetail(Record.Inserted, person);
                    }
                    else
                    {
                        ///existing person
                        if(PersonChanged(rec05, person) || (rec05.Address1.Trim() == String.Empty))
                        {
                            ///person.ModifiedStamp is Nullable<System.DataTime>
                            ///and I need to convert to DateTime for date comparison
                            ///so I'm subtracting a year to make it older than 
                            ///the incoming input if it is null. Better solution???
                            TimeSpan timespan = new TimeSpan(-365, 0, 0, 0);
                            DateTime d1 = person.ModifiedStamp ?? DateTime.Now.Add(timespan);

                            if (DateTime.Compare(d1, rec01.CreationStamp) < 0)
                            { 
                                LogPersonLoad(person.KCPersonID, ChangeType.UpdatedPerson);

                                LogPersonLoadDetail(Record.Deleted, person);//log old record
                                
                                EpcDemographicChanges(person, rec05); //call before PersonUpdate

                                PersonUpdate(rec05, ref person); //update person

                                LogPersonLoadDetail(Record.Inserted, person); //log new record
                            }
                            else
                            {
                                string desc = String.Format("Person.ModifiedStamp: {0} was modified later than Rec01.CreationStamp: {1}", person.ModifiedStamp, rec01.CreationStamp);
                                LogPersonLoad(person.KCPersonID, ChangeType.SkippedPerson, desc);
                            }                            
                        }
                        else
                        {
                            LogPersonLoad(person.KCPersonID, ChangeType.UnchangedPerson);
                        }                        
                    }  
                    db.SaveChanges();
                }
            }
            catch
            { throw; }
        }

        private static KCPerson PersonCreate(RecordType05 rec05)
        {
            try
            {
                KCPerson person = new KCPerson();
                person.KCPersonID = Guid.NewGuid();
                person.IsActive = (int)PersonStatus.Inactive; // why is this inactive?

                person.PersonID = rec05.PersonID.Trim();
                person.SSN = rec05.SSN_FTIN.Trim();
                person.LastName = rec05.LastName.Trim();
                person.FirstName = rec05.FirstName.Trim();
                person.MiddleName = rec05.MiddleName.Trim();// Exception for middle name
                person.Suffix = rec05.Suffix.Trim();
                
                person.Address = rec05.Address1.Trim();
                if (!String.IsNullOrEmpty(rec05.Address2.Trim()))
                {
                    person.Address += (Environment.NewLine + rec05.Address2.Trim());
                }
                if (!String.IsNullOrEmpty(rec05.Address3.Trim()))
                {
                    person.Address += (Environment.NewLine + rec05.Address3.Trim());
                }
                
                person.AddressStamp = rec01.CreationStamp;

                person.InvalidAddress = false;
                person.City = rec05.City.Trim();
                person.State = rec05.State.Trim();
                person.Zip = rec05.Zip.Trim();
                person.Country = rec05.Country.Trim();

                person.HomePhone = rec05.HomePhone.Trim();
                person.CellPhone = rec05.CellPhone.Trim();
                person.WorkPhone = rec05.WorkPhone.Trim();
                person.WorkExtension = rec05.WorkExtension.Trim();
                person.PhoneStamp = rec01.CreationStamp;
                person.Pager = rec05.PagerNumber.Trim();

                person.DateOfBirth = rec05.DateofBirth;
                person.DateOfDeath = rec05.DateofDeath;
                person.DODStamp = rec01.CreationStamp;
                person.FVI = rec05.FamilyViolenceInd.Trim() == "Y" ? true : false;
                person.eMail = rec05.EmailAddress.Trim();
                person.EmailStamp = rec01.CreationStamp;
                person.BillingEmail = rec05.NotificationEmailAddress.Trim();
                person.BillingEmailStamp = rec01.CreationStamp;

                byte deliveryMethodID;
                if (byte.TryParse(rec05.DeliveryMethod.Trim(), out deliveryMethodID))
                {
                    if (person.KCDeliveryMethodID != deliveryMethodID)
                    {
                        person.KCDeliveryMethodID = deliveryMethodID;
                        person.DeliveryMethodStamp = rec01.CreationStamp;
                    }
                }

                if (!String.IsNullOrEmpty(rec05.IVDStatus.Trim()))
                    person.IVDStatus = rec05.IVDStatus == "Y" ? true : false;
                else
                    person.IVDStatus = null;

                person.ModifiedBy = "ArpLoad";
                person.ModifiedStamp = rec01.CreationStamp;
                person.CreationStamp = rec01.CreationStamp;

                return person;
            }
            catch
            {
                throw;
            }
        }

        private enum AccountType : byte { Savings, Checking, Unknown, EPC};

        private static void EpcDemographicChanges(KCPerson person, RecordType05 rec05)
        {
            #region Comments
            /// if person has an relia card or epc card
            /// then insert a record into the demographic
            /// changes table so the change can go to US Bank.
            /// Other changes don't need to be recorded 
            /// because they will just go back to CHARTS
            /// where the input file came from
            ///
            #endregion
            using(var db = new PersonDBContext())
            {
                KCPersonFinancial personFinancial = db.KCPersonFinancials.FirstOrDefault(x => x.KCPersonID == person.KCPersonID && x.KCAccountTypeID == (byte)AccountType.EPC);
                if (personFinancial != null) // if person has epc card
                {
                    if(!IsSameAddress(person.Address, rec05.Address1, rec05.Address2, rec05.Address3))
                    {
                      /// update KCDemographics
                      /// 
                        KCTrackDemographicChanx personDemoChanges = new KCTrackDemographicChanx();
                        personDemoChanges.KCPersonID = person.KCPersonID;
                        personDemoChanges.Account = personFinancial.Account;
                        personDemoChanges.RecordType = 230;
                        personDemoChanges.FieldName = "Address";
                        personDemoChanges.FieldData = rec05.Address1 + rec05.Address2 + rec05.Address3;
                        personDemoChanges.Exported = false;
                        personDemoChanges.ModifiedStamp = DateTime.Now;
                        personDemoChanges.ModifiedBy = "ARPLOAD";
                        db.KCTrackDemographicChanges.Add(personDemoChanges);
                    }
                    if(person.City != rec05.City)
                    {
                        KCTrackDemographicChanx personDemoChanges = new KCTrackDemographicChanx();
                        personDemoChanges.KCPersonID = person.KCPersonID;
                        personDemoChanges.Account = personFinancial.Account;
                        personDemoChanges.RecordType = 230;
                        personDemoChanges.FieldName = "City";
                        personDemoChanges.FieldData = rec05.City;
                        personDemoChanges.Exported = false;
                        personDemoChanges.ModifiedStamp = DateTime.Now;
                        personDemoChanges.ModifiedBy = "ARPLOAD";
                        db.KCTrackDemographicChanges.Add(personDemoChanges);
                    }
                    if (person.State != rec05.State)
                    {
                        KCTrackDemographicChanx personDemoChanges = new KCTrackDemographicChanx();
                        personDemoChanges.KCPersonID = person.KCPersonID;
                        personDemoChanges.Account = personFinancial.Account;
                        personDemoChanges.RecordType = 230;
                        personDemoChanges.FieldName = "State";
                        personDemoChanges.FieldData = rec05.State;
                        personDemoChanges.Exported = false;
                        personDemoChanges.ModifiedStamp = DateTime.Now;
                        personDemoChanges.ModifiedBy = "ARPLOAD";
                        db.KCTrackDemographicChanges.Add(personDemoChanges);
                    }
                    if(person.Zip != rec05.Zip)
                    {
                        KCTrackDemographicChanx personDemoChanges = new KCTrackDemographicChanx();
                        personDemoChanges.KCPersonID = person.KCPersonID;
                        personDemoChanges.Account = personFinancial.Account;
                        personDemoChanges.RecordType = 230;
                        personDemoChanges.FieldName = "Zip";
                        personDemoChanges.FieldData = rec05.Zip;
                        personDemoChanges.Exported = false;
                        personDemoChanges.ModifiedStamp = DateTime.Now;
                        personDemoChanges.ModifiedBy = "ARPLOAD";
                        db.KCTrackDemographicChanges.Add(personDemoChanges);
                    }

                    db.SaveChanges();
                }
                else
                {
                    //don't do anything and continue
                }
            }
        }

        private static void LogPersonLoad(Guid EntityID, string changeType)
        {
            string sql = String.Format("INSERT KCPersonLoad"
                                        + " (KCPersonID, ModifiedDate, PersonStatus, Description)"
                                        + " VALUES ('{0}','{1}','{2}',{3})"
                                        + " SET @KCPersonLoadId = SCOPE_IDENTITY();"
                                        , EntityID
                                        , DateTime.Now
                                        , changeType
                                        , "NULL"
                                        );
            scriptLog.WriteLine(sql);
        }

        private static void LogPersonLoad(Guid EntityID, string changeType, string desc)
        {
            string sql = String.Format("INSERT KCPersonLoad"
                                        + " (KCPersonID, ModifiedDate, PersonStatus, Description)"
                                        + " VALUES ('{0}','{1}','{2}','{3}')"
                                        + " SET @KCPersonLoadId = SCOPE_IDENTITY();"
                                        , EntityID
                                        , DateTime.Now
                                        , changeType
                                        , desc
                                        );

            scriptLog.WriteLine(sql);
        }

        private static void LogPersonLoad(string changeType, string desc)
        {
            string sql = String.Format("INSERT KCPersonLoad"
                                        + " (KCPersonID, ModifiedDate, PersonStatus, Description)"
                                        + " VALUES ('{0}','{1}','{2}','{3}')"
                                        + " SET @KCPersonLoadId = SCOPE_IDENTITY();"
                                        , DBNull.Value
                                        , DateTime.Now
                                        , changeType
                                        , desc
                                        );

            scriptLog.WriteLine(sql);
        }

        private static void LogPersonLoadDetail(string thisRecordIs, KCPerson person)
        {
            //Handle Apostrophes for SQL script
            string firstName = "";
            string lastName = "";
            string notes = "";

            if (person.FirstName != null)
                firstName = person.FirstName.Replace("'", "''");

            if (person.LastName != null)
                lastName = person.LastName.Replace("'", "''");

            if (person.Notes != null)
                notes = person.Notes.Replace("'", "''");

            ///Person Load Detail record.  This table
            /// doesn't exist it will have to be
            /// created before the generated script is run
            string sql = "INSERT [KCPersonLoadDetail"
                + "(KCPersonLoadID"
                + ",RecordStatus"
                + ",KCPersonID"
                + ",SSN"
                + ",LastName"
                + ",FirstName"
                + ",MiddleName"
                + ",Suffix"
                + ",Address"
                + ",City"
                + ",State"
                + ",Zip"
                + ",HomePhone"
                + ",CellPhone"
                + ",WorkPhone"
                + ",WorkExtension"
                + ",eMail"
                + ",Country"
                + ",DateOfBirth"
                + ",DateOfDeath"
                + ",Notes"
                + ",ModifiedStamp"
                + ",ModifiedBy"
                + ",PINNumber"
                + ",PersonID"
                + ",Pager"
                + ",AlternateEmail"
                + ",BillingEmail"
                + ",KCDeliveryMethodID"
                + ",FVI"
                + ",IsActive"
                + ",InactiveDate"
                + ",InvalidAddress"
                + ",DenyWebAccess"
                + ",ChangeReasonCode"
                + ",PaymentNotification"
                + ",CreationStamp"
                + ",KCPaymentMethodID"
                + ",AddressStamp"
                + ",PhoneStamp"
                + ",EmailStamp"
                + ",BillingEmailStamp"
                + ",DeliveryMethodStamp"
                + ",DODStamp"
                + ",PaymentMethodStamp"
                + ",SignMeUpForEbs"
                + ",IVDStatus)"
                + " VALUES"
                + "( @KCPersonLoadID"
                + ",'" + thisRecordIs + "'"
                + ",'" + person.KCPersonID + "'"
                + ",'" + person.SSN + "'"
                + ",'" + person.LastName + "'"
                + ",'" + person.FirstName + "'"
                + ",'" + person.MiddleName + "'"
                + ",'" + person.Suffix + "'"
                + ",'" + person.Address + "'"
                + ",'" + person.City + "'"
                + ",'" + person.State + "'"
                + ",'" + person.Zip + "'"
                + ",'" + person.HomePhone + "'"
                + ",'" + person.CellPhone + "'"
                + ",'" + person.WorkPhone + "'"
                + ",'" + person.WorkExtension + "'"
                + ",'" + person.eMail + "'"
                + ",'" + person.Country + "'"
                + ",'" + person.DateOfBirth + "'"
                + ",'" + person.DateOfDeath + "'"
                + ",'" + person.Notes + "'"
                + ",'" + person.ModifiedStamp + "'"
                + ",'" + person.ModifiedBy + "'"
                + ",'" + person.PINNumber + "'"
                + ",'" + person.PersonID + "'"
                + ",'" + person.Pager + "'"
                + ",'" + person.AlternateEmail + "'"
                + ",'" + person.BillingEmail + "'"
                + ",'" + person.KCDeliveryMethodID + "'"
                + ",'" + person.FVI + "'"
                + ",'" + person.IsActive + "'"
                + ",'" + person.InactiveDate + "'"
                + ",'" + person.InvalidAddress + "'"
                + ",'" + person.DenyWebAccess + "'"
                + ",'" + person.ChangeReasonCode + "'"
                + ",'" + person.PaymentNotification + "'"
                + ",'" + person.CreationStamp + "'"
                + ",'" + person.KCPaymentMethodID + "'"
                + ",'" + person.AddressStamp + "'"
                + ",'" + person.PhoneStamp + "'"
                + ",'" + person.EmailStamp + "'"
                + ",'" + person.BillingEmailStamp + "'"
                + ",'" + person.DeliveryMethodStamp + "'"
                + ",'" + person.DODStamp + "'"
                + ",'" + person.PaymentMethodStamp + "'"
                + ",'" + person.SignMeUpForEbs + "'"
                + ",'" + person.IVDStatus + "'"
                + ");";

            scriptLog.WriteLine(sql);
        }

        private static bool PersonChanged(RecordType05 rec05, KCPerson person)
        {
            /// I need to make a change to see if this could be empty or null when comparing
            /// because it will affecte my changes when null is not equal to empty.


            if ((person.PersonID ?? "") == rec05.PersonID.Trim()
                && (person.SSN ?? "") == rec05.SSN_FTIN.Trim()
                && (person.LastName ?? "") == rec05.LastName.Trim()
                && (person.FirstName ?? "") == rec05.FirstName.Trim()
                && (person.MiddleName ?? "") == rec05.MiddleName.Trim()
                && (person.Suffix ?? "") == rec05.Suffix.Trim() 
                && (IsSameAddress(person.Address, rec05.Address1.Trim(), rec05.Address2.Trim(), rec05.Address3.Trim()))
                && (person.City ?? "") == rec05.City.Trim()
                && (person.State ?? "") == rec05.State.Trim()
                && (person.Zip ?? "") == rec05.Zip.Trim()
                && (person.HomePhone ?? "") == rec05.HomePhone.Trim()
                && (person.CellPhone ?? "") == rec05.CellPhone.Trim()
                && (person.WorkPhone ?? "") == rec05.WorkPhone.Trim()
                && (person.WorkExtension ?? "") == rec05.WorkExtension.Trim()
                && (person.Pager ?? "") == rec05.PagerNumber.Trim()
                && (person.DateOfBirth) == rec05.DateofBirth
                && (person.DateOfDeath) == rec05.DateofDeath
                && (person.Country ?? "") == rec05.Country.Trim()
                && (person.FVI) == (rec05.FamilyViolenceInd.Trim() == "Y" ? true : false)
                && (person.eMail ?? "") == rec05.EmailAddress.Trim()
                && (person.BillingEmail ?? "") == rec05.NotificationEmailAddress.Trim()
                && (person.KCDeliveryMethodID.ToString()) == rec05.DeliveryMethod.Trim()
                && (person.IVDStatus) == (rec05.IVDStatus == "Y" ? true : false)
                )
                return false;
            else
                return true;
        }

        private static bool IsSameAddress(string current_address, string address1, string address2, string address3)
        {
            try
            {
                /// The address field from the database has one column
                /// while the value from the file is split in three
                /// fields (addres1, address2 and adress3)
                /// 

                // New address
                string new_address1 = address1.Trim();
                string new_address2 = address2.Trim();
                string new_address3 = address3.Trim();
                StringBuilder sbNewAddress = new StringBuilder();

                sbNewAddress.Append(new_address1);
                if (!String.IsNullOrEmpty(new_address2))
                    sbNewAddress.Append(new_address2);

                if (!String.IsNullOrEmpty(new_address3))
                    sbNewAddress.Append(new_address3);

                string leanNewAddr = sbNewAddress.ToString().Replace(" ", String.Empty);

            
                // Current address
                string leanCurrAddr = String.Empty;

                if(!String.IsNullOrEmpty(current_address))
                {
                    string[] addresses = current_address.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);                

                    foreach (string addr in addresses)
                    {
                        leanCurrAddr += addr;
                    }
                    leanCurrAddr = leanCurrAddr.Replace(" ", String.Empty);
                }            

                // compare
                if (leanCurrAddr == leanNewAddr)
                    return true;
                else
                    return false;
            }
            catch(NullReferenceException nullref)
            {
                string err = nullref.ToString();
                throw;
            }
        }

        private static void PersonUpdate(RecordType05 rec05, ref KCPerson person)
        {
            if (!String.IsNullOrEmpty(rec05.PersonID.Trim()))
                person.PersonID = rec05.PersonID.Trim();

            if (!String.IsNullOrEmpty(rec05.SSN_FTIN.Trim()))
                person.SSN = rec05.SSN_FTIN.Trim();

            if (!String.IsNullOrEmpty(rec05.LastName.Trim()))
                person.LastName = rec05.LastName.Trim();

            if (!String.IsNullOrEmpty(rec05.FirstName.Trim()))
                person.FirstName = rec05.FirstName.Trim();

             person.MiddleName = rec05.MiddleName.Trim();// Exception for middle name

            if (!String.IsNullOrEmpty(rec05.Suffix.Trim()))
                person.Suffix = rec05.Suffix.Trim();

            if (!String.IsNullOrEmpty(rec05.Address1.Trim()))
            {
                if (!IsSameAddress(person.Address, rec05.Address1, rec05.Address2, rec05.Address3))
                {
                    person.Address = rec05.Address1.Trim();

                    if (!String.IsNullOrEmpty(rec05.Address2.Trim()))
                    {
                        person.Address += (Environment.NewLine + rec05.Address2.Trim());
                    }
                    if (!String.IsNullOrEmpty(rec05.Address3.Trim()))
                    {
                        person.Address += (Environment.NewLine + rec05.Address3.Trim());
                    }

                    person.InvalidAddress = false;
                    person.AddressStamp = rec01.CreationStamp;

                }
            }
            else
            {
                person.InvalidAddress = true;
            }

            if (!String.IsNullOrEmpty(rec05.City.Trim()))
                person.City = rec05.City.Trim();

            if(!String.IsNullOrEmpty(rec05.State.Trim()))
                person.State = rec05.State.Trim();

            if (!String.IsNullOrEmpty(rec05.Zip.Trim()))
                person.Zip = rec05.Zip.Trim();

            if (!String.IsNullOrEmpty(rec05.HomePhone.Trim()))
            {
                if (person.HomePhone != rec05.HomePhone.Trim())
                {
                    person.HomePhone = rec05.HomePhone.Trim();
                    person.PhoneStamp = rec01.CreationStamp;
                }
            }

            if (!String.IsNullOrEmpty(rec05.CellPhone.Trim()))
            {
                if (person.CellPhone != rec05.CellPhone.Trim())
                {
                    person.CellPhone = rec05.CellPhone.Trim();
                    person.PhoneStamp = rec01.CreationStamp;
                }
            }

            if (!String.IsNullOrEmpty(rec05.WorkPhone.Trim()) ||!String.IsNullOrEmpty(rec05.WorkExtension.Trim()))
            {
                bool workPhoneChanged = false;

                if (!String.IsNullOrEmpty(rec05.WorkPhone.Trim()))
                {
                    if (person.WorkPhone != rec05.WorkPhone.Trim())
                    {
                        person.WorkPhone = rec05.WorkPhone.Trim();
                        workPhoneChanged = true;
                    }
                }

                if (!String.IsNullOrEmpty(rec05.WorkExtension.Trim()))
                {
                    if (person.WorkExtension != rec05.WorkExtension.Trim())
                    {
                        person.WorkExtension = rec05.WorkExtension.Trim();
                        workPhoneChanged = true;
                    }
                }

                if (workPhoneChanged)
                    person.PhoneStamp = rec01.CreationStamp;
            }            

            if (!String.IsNullOrEmpty(rec05.PagerNumber.Trim()))
                person.Pager = rec05.PagerNumber.Trim();

            if (!String.IsNullOrEmpty(rec05.DateofBirth.ToString()))
                person.DateOfBirth = rec05.DateofBirth;

            if (!String.IsNullOrEmpty(rec05.DateofDeath.ToString()))
            {
                person.DateOfDeath = rec05.DateofDeath;
                person.DODStamp = rec01.CreationStamp;
            }

            if (!String.IsNullOrEmpty(rec05.Country.Trim()))
                person.Country = rec05.Country.Trim();

            if (!String.IsNullOrEmpty(rec05.FamilyViolenceInd.Trim()))
                person.FVI = rec05.FamilyViolenceInd.Trim() == "Y" ? true : false;

            if (!String.IsNullOrEmpty(rec05.EmailAddress.Trim())
                && (person.eMail != rec05.EmailAddress.Trim()))
            {
                    person.eMail = rec05.EmailAddress.Trim();
                    person.EmailStamp = rec01.CreationStamp;
            }

            if (!String.IsNullOrEmpty(rec05.NotificationEmailAddress.Trim())
                && (person.BillingEmail != rec05.NotificationEmailAddress.Trim()))
            {
                person.BillingEmail = rec05.NotificationEmailAddress.Trim();
                person.BillingEmailStamp = rec01.CreationStamp;
            }

            byte deliveryMethodID;
            if (byte.TryParse(rec05.DeliveryMethod.Trim(), out deliveryMethodID))
            {
                if (person.KCDeliveryMethodID != deliveryMethodID)
                {
                    person.KCDeliveryMethodID = deliveryMethodID;
                    person.DeliveryMethodStamp = rec01.CreationStamp;
                }
            }

            if (!String.IsNullOrEmpty(rec05.IVDStatus.Trim()))
                person.IVDStatus = rec05.IVDStatus == "Y" ? true : false;
            else
                person.IVDStatus = null;

            person.ModifiedBy = "ArpLoad";
            person.ModifiedStamp = rec01.CreationStamp;
        }

        private static void ProcessCase(RecordType07 rec07)
        {
            try
            {
                using(var db = new PersonDBContext())
                {
                    Guid EntityID = Guid.Empty;
                    var person = db.KCPersons.SingleOrDefault(x => x.PersonID == rec07.PersonID);
                    if (person == null)
                    {
                        LogPersonLoad(EntityID, ChangeType.SkippedCase, rec07.PersonID + " not found");                        
                    }
                    else
                    {
                        EntityID = person.KCPersonID;
                        int seqNum;
                        string altCaseNumber;

                        if (int.TryParse(rec07.SeqNum.Trim(), out seqNum))
                        {
                            /// The alt case number can be a little messed up.  It can be equal to all these fields below or it can also be equal to
                            /// the IVDCase Number.  Therefore when both values are present in the file we have to save two records in the table
                            /// because the architecture in the KCCases table only allows for one altCaseNumber.                       
                            altCaseNumber = rec07.Court.Trim() + rec07.CountyNumber.Trim() + rec07.CaseType.Trim() + rec07.YearCode + seqNum.ToString();
                        }
                        else
                        {
                            throw (new CustomException(
                                        String.Format("Rec 07: {0} Sequence Number is not an integer: {1}."
                                            , rec07.PersonID
                                            , rec07.SeqNum))
                                    );
                        }

                        PersonCaseUpdate(rec07, EntityID, altCaseNumber);

                        //Check for the IVD number and save it in a new record
                        if (!String.IsNullOrEmpty(rec07.IVDCaseNumber.Trim()))
                            PersonCaseUpdate(rec07, EntityID, rec07.IVDCaseNumber.Trim());
                        //////////////////////////////////////////////////////////
                        /// EntityID represent either PersonID or OrganizationID
                        /// Adding the record case to the KCCases table needs to
                        /// go here once issue with KCPersons_KCCases is resolved
                        /*
                        else
                        {
                            var org = db.KCOrganizations.SingleOrDefault(x => x.StateID == rec07.PersonID);
                            if (org != null)
                                EntityID = org.KCOrganizationID;                        
                        }

                        if (EntityID != Guid.Empty)
                        {
                            //add the KCCase record goes here
                        } 
                        */
                        //////////////////////////////////////////////////////////                        
                    }
                }              
            }
            catch
            { throw; }
        }

        private static void PersonCaseUpdate(RecordType07 rec07, Guid EntityID, string altCaseNumber)
        {
            //check to see if the IVDCaseNumber matches a record in the AltCaseNumber
            using(var db = new PersonDBContext())
            {
                IQueryable<KCCas> personCases = db.KCCases.Where(x => x.KCPersonID == EntityID
                                                && x.CaseNumber == rec07.CourtCaseId.Trim()
                                                && x.AltCaseNumber == altCaseNumber);

                if (personCases.Count() == 0)
                {
                    //Add it
                    KCCas personCase = new KCCas();
                    personCase.KCCaseID = Guid.NewGuid();
                    personCase.KCPersonID = EntityID;
                    personCase.CaseNumber = rec07.CourtCaseId.Trim();
                    personCase.CountyID = rec07.CountyNumber.Trim();
                    personCase.Caption = rec07.CaseCaption.Trim();
                    personCase.OtherID = rec07.OldCaseNumber.Trim();
                    personCase.ModifiedStamp = rec07.ModifiedStamp;// we do have a modified stamp on here should I compare them before I change
                    personCase.ModifiedBy = "ArpLoad";
                    personCase.IsPayor = String.Compare(rec07.PayorPayee, "PAYOR", true) == 0 ? true : false;
                    personCase.AltCaseNumber = altCaseNumber;
                    personCase.ToBeDeleted = false;

                    db.KCCases.Add(personCase);
                    LogPersonLoad(EntityID, ChangeType.NewCase, altCaseNumber);
                }
                else
                {
                    //Update it
                    foreach (var personCase in personCases)
                    {
                        if (PersonCaseChanged(personCase, rec07, altCaseNumber))
                        {
                            personCase.CountyID = rec07.CountyNumber.Trim();
                            personCase.Caption = rec07.CaseCaption.Trim();
                            personCase.OtherID = rec07.OldCaseNumber.Trim();
                            personCase.ModifiedStamp = rec07.ModifiedStamp;
                            personCase.ModifiedBy = "ArpLoad";
                            personCase.IsPayor = String.Compare(rec07.PayorPayee, "PAYOR", true) == 0 ? true : false;
                            personCase.AltCaseNumber = altCaseNumber;
                            personCase.ToBeDeleted = false;

                            LogPersonLoad(EntityID, ChangeType.UpdatedCase, altCaseNumber);
                        }
                        else
                        {
                            if (DataFile.IsWeekly) personCase.ToBeDeleted = false;
                            LogPersonLoad(EntityID, ChangeType.UnchangedCase, "match");
                        }
                    }
                }
                db.SaveChanges();
            }
            
        }

        private static bool PersonCaseChanged(KCCas personCase, RecordType07 rec07, string altCaseNumber)
        {
            if ((personCase.CountyID ?? "") == rec07.CountyNumber.Trim()
                && (personCase.Caption ?? "") == rec07.CaseCaption.Trim()
                && (personCase.OtherID ?? "") == rec07.OldCaseNumber.Trim()
                //&& (personCase.ModifiedStamp) == rec07.ModifiedStamp
                && (personCase.IsPayor) == (String.Compare(rec07.PayorPayee, "PAYOR", true) == 0 ? true : false)
                && (personCase.AltCaseNumber ?? "") == altCaseNumber)
                return false;
            else
                return true;
        }

        private static void ProcessOrganization(RecordType05 rec05)
        {
            try
            {
                using(var db = new PersonDBContext())
                {
                    var org = db.KCOrganizations.SingleOrDefault(x => x.StateID == rec05.PersonID);

                    if(org == null)
                    { 
                        //Add Organization
                        org = new KCOrganization();
                        org.Name = rec05.LastName.Trim() + rec05.FirstName.Trim() + rec05.MiddleName.Trim();
                        org.Address = rec05.Address1.Trim() + rec05.Address2.Trim() + rec05.Address3.Trim();
                        org.City = rec05.City.Trim();
                        org.State = rec05.State.Trim();
                        org.Zip = rec05.Zip.Trim();
                        org.FTIN = rec05.SSN_FTIN.Trim();
                        org.PhoneNumber = rec05.HomePhone.Trim();
                        org.ModifiedStamp = DateTime.Now;
                        org.ModifiedBy = "ArpLoad";
                        org.IsSDU = rec05.SDUInd == "Y"? true : false;
                    }
                    else
                    {
                        //update Organization
                        org.Name = rec05.LastName.Trim() + rec05.FirstName.Trim() + rec05.MiddleName.Trim();
                        org.Address = rec05.Address1.Trim() + rec05.Address2.Trim() + rec05.Address3.Trim();
                        org.City = rec05.City.Trim();
                        org.State = rec05.State.Trim();
                        org.Zip = rec05.Zip.Trim();
                        org.FTIN = rec05.SSN_FTIN.Trim();
                        org.PhoneNumber = rec05.HomePhone.Trim();
                        org.ModifiedStamp = DateTime.Now;
                        org.ModifiedBy = "ArpLoad";
                        org.IsSDU = rec05.SDUInd == "Y" ? true : false;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        private static void LogRecordCounts()
        {
            progLog.WriteLine("Total Record count : " + recordCount);
            progLog.WriteLine("Record count(04): " + recordCount04);
            progLog.WriteLine("Record count(05): " + recordCount05);
            progLog.WriteLine("Record count(07): " + recordCount07);
        }

        private static void EmailNotification(int _exitCode)
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(ConfigurationManager.AppSettings["FromEmail"].ToString());

            if (_exitCode == (int)ExitCode.Success)
            {
                string notifyEmail = ConfigurationManager.AppSettings["NotifyEmail"].ToString();
                string fileDir = ConfigurationManager.AppSettings["DataFileDirPath"].ToString();

                mail.Subject = "PersonLoad: Completed Successfully";
                mail.To.Add(notifyEmail);
                mail.Body = "PersonLoad job processed <b>successfully</b>. <br /><br />";
                mail.Body += "Data file Directory: " + fileDir + " <br />";
                mail.Body += "See attached log file for additional details.";
            }
            else
            {
                mail.Subject = "PersonLoad Failed";
                mail.To.Add(ConfigurationManager.AppSettings["ItStaffEmail"].ToString());
                mail.Body = "PersonLoad job <b>failed</b> with exit code " + _exitCode + ".<br /><br />";
                mail.Body += "See attached log file for additional details.";
            }

            mail.IsBodyHtml = true;

            bool fileExists = File.Exists(progLog.FilePath);
            if (fileExists)
            {
                mail.Attachments.Add(new Attachment(progLog.FilePath));
            }

            SmtpClient smtp = new SmtpClient(ConfigurationManager.AppSettings["MailHost"].ToString());
            smtp.Send(mail);
        }
  
    }
}