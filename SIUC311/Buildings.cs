using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIUC311
{
    class Buildings
    {
        private static List<Building> List = new List<Building>();
        private static string url_string = "http://siu.edu/maps/building_photos/";
        private static string hall_string = "The eleven 3-story residence halls in this area offer suite-style rooms, social corridor lounges on each floor and laundry on the lower level. Accessible rooms are available for students with mobility impairments.";
        public Buildings()
        {   
            List.Add(new Building("Academic", "Wham Education", 37.717335, -89.221685, "-", url_string + "0044.jpg"));
            List.Add(new Building("Academic", "Lawson Hall", 37.716064, -89.222855, "Lecture halls.", url_string + "0047.jpg"));
            List.Add(new Building("Academic", "Rehn Hall", 37.716698, -89.222804, "-", url_string + "0046.jpg"));
            List.Add(new Building("Academic", "Life Science II", 37.715255, -89.222837, "-", url_string + "0052.jpg"));
            List.Add(new Building("Academic", "Communications Building", 37.715098, -89.225247, "Cinema and Photography, McCleod, Kleinau Theatre, Daily Egyptian, Speech Communications, WSIU", url_string + "0051.jpg"));
            List.Add(new Building("Academic", "Life Science III", 37.714615, -89.223074, "-", url_string + "0053.jpg"));
            List.Add(new Building("Academic", "Lindegren Hall", 37.715353, -89.221706, "-", url_string + "0017.jpg"));
            List.Add(new Building("Academic", "Agriculture Building", 37.712964, -89.22243, "-", url_string + "0026.jpg"));
            List.Add(new Building("Academic", "Pulliam Hall", 37.718022, -89.220061, "Clock tower.", url_string + "0023.jpg"));
            List.Add(new Building("Academic", "Morris Library", 37.71526, -89.220472, "Vast collection of books, magazines, articles, research, and information.", url_string + "0025.jpg"));
            List.Add(new Building("Academic", "Lesar Law Building", 37.713545, -89.230053, "Law school", url_string + "0048.jpg"));
            List.Add(new Building("Housing", "Smith Hall", 37.712683, -89.226109, hall_string, url_string + "0037.jpg"));
            List.Add(new Building("Housing", "Warren Hall", 37.712047, -89.226364, hall_string, url_string + "0036.jpg"));
            List.Add(new Building("Housing", "Kellog Hall", 37.711485, -89.225935, hall_string, url_string + "0035.jpg"));
            List.Add(new Building("Housing", "Felts Hall", 37.711016, -89.225438, hall_string, url_string + "0034.jpg"));
            List.Add(new Building("Housing", "Brown Hall", 37.710241, -89.225548, hall_string, url_string + "0033.jpg"));
            List.Add(new Building("Housing", "Steagall Hall", 37.709575, -89.225371, hall_string, url_string + "0032.jpg"));
            List.Add(new Building("Housing", "Bowyer Hall", 37.709469, -89.224395, hall_string, url_string + "0031.jpg"));
            List.Add(new Building("Housing", "Pierce Hall", 37.710277, -89.224623, hall_string, url_string + "0029.jpg"));
            List.Add(new Building("Housing", "Lentz Hall", 37.71102, -89.22421, "Lentz Hall, the commons building for West Campus, houses the area office, a dining hall, Lakeside Express, a fitness center, mailroom, student lounge, computer lab and an additional accessible laundry room.", url_string + "0027.jpg"));
            List.Add(new Building("Housing", "Bailey Hall", 37.711491, -89.223282, hall_string, url_string + "0028.jpg"));
            List.Add(new Building("Housing", "Baldwin Hall", 37.711822, -89.224915, hall_string, url_string + "0039.jpg"));
            List.Add(new Building("Housing", "Abbott Hall", 37.712529, -89.22531, hall_string, url_string + "0038.jpg"));
            List.Add(new Building("Administration", "Wakeland Hall", 37.712679, -89.230116, "-", url_string + "0113.jpg"));
            List.Add(new Building("Administration", "Kesnar Hall", 37.712217, -89.230489, "-", url_string + "0112.jpg"));
            List.Add(new Building("Administration", "Beimfohr Hall", 37.71213, -89.231055, "University Communications", url_string + "0115.jpg"));
            List.Add(new Building("Administration", "Kaplan Hall", 37.712616, -89.229475, "-", url_string + "0114.jpg"));
            List.Add(new Building("Administration", "Colyer Hall", 37.711703, -89.230368, "Alumni and Foundation", url_string + "0111.jpg"));
            List.Add(new Building("Administration", "Miles Hall", 37.710814, -89.230001, "-", url_string + "0108.jpg"));
            List.Add(new Building("Administration", "Crawford Hall", 37.710954, -89.229336, "-", url_string + "0107.jpg"));
            List.Add(new Building("Academic", "Fulkerson Hall", 37.711928, -89.229542, "-", url_string + "0102.jpg"));
            List.Add(new Building("Academic", "Paul Simon Public Policy Institute", 37.71157, -89.220841, "-", url_string + "0066.jpg"));
            List.Add(new Building("Academic", "Engineering A Wing", 37.7099, -89.220498, "Advanced Friction, Dental Hygiene, Engineering", url_string + "0071.jpg"));
            List.Add(new Building("Academic", "Engineering B Wing", 37.709764, -89.221029, "Central Research Shop", url_string + "0071.jpg"));
            List.Add(new Building("Academic", "Engineering C Wing", 37.709176, -89.220846, "Allied Health, Applied Atrs and Sciences, Information Systems", url_string + "0071.jpg"));
            List.Add(new Building("Academic", "Engineering D Wing", 37.70935, -89.219969, "Civil and Environmental, Technology, Physical Therapy Assistant Program", url_string + "0071.jpg"));
            List.Add(new Building("Academic", "Engineering E Wing", 37.709844, -89.21976, "Engineering, Materials, Mining and Mineral", url_string + "0072.jpg"));
            List.Add(new Building("Recreation/Sports", "SIU Arena", 37.708317, -89.218691, "The SIU Arena is a 8,339-seat multi-purpose arena, on the campus of Southern Illinois University, in Carbondale, Illinois, United States. Construction on the arena began in the spring of 1962 and took nearly two years to complete. It was completed in 1964 and is the home of the SIU Salukis basketball team.", url_string + "0041.jpg"));
            List.Add(new Building("Academic", "Lingle Hall", 37.709134, -89.218545, "-", url_string + "0040.jpg"));
            List.Add(new Building("Academic", "Troutt-Wittman Center", 37.709513, -89.218545, "The Troutt-Wittmann Academic and Training Center features a fitness center as well as a mix of quiet areas in which to study and be tutored, along with high-speed internet hook-ups and a combination of traditional and modern educational resources, from books to study guides and computer labs.", url_string + "9999.jpg"));
            List.Add(new Building("Recreation/Sports", "Saluki Stadium", 37.708554, -89.216953, "The Southern Illinois football team moved into the brand-new $29.9 million Saluki Stadium for the 2010 season. Seating includes 1,080 prime chairback seats as well as spots on the grass berm enclosing the north end zone.", url_string + "0085.jpg"));
            List.Add(new Building("Recreation/Sports", "Abe Martin Field", 37.704955, -89.221502, "Abe Martin Field, home of SIU baseball. ", url_string + "1901.jpg"));
            List.Add(new Building("Recreation/Sports", "Saluki Track and Field Complex", 37.704955, -89.221502, "A regulation NCAA competition 400-meter track with a state-of-the art, full-depth polyurethane synthetic track surface system.", url_string + "1996.jpg "));
            List.Add(new Building("Academic", "Neckers Building", 37.71162, -89.219167, "Home of the Math and Physics departments.", url_string + "0063.jpg"));
            List.Add(new Building("Administration", "Student Center", 37.713163, -89.218525, "Many options for food including Old Mayne. Student ID office. Ballrooms for special events.", url_string + "0045.jpg"));
            List.Add(new Building("Academic", "Faner Hall", 37.715268, -89.218874, "Home of Computer Science, English, Museum, and other department offices.", url_string + "0096.jpg"));
            List.Add(new Building("Academic", "Parkinson Laboratory", 37.714971, -89.218009, "-", url_string + "0004.jpg"));
            List.Add(new Building("Administration", "Anthony Hall", 37.715206, -89.216748, "Chancellor, Provost, Vice Chancellor For Research And Graduate Dean, Budget, Dean Of Students", url_string + "0005.jpg"));
            List.Add(new Building("Academic", "Allyn Hall", 37.715474, -89.217732, "Art and Design", url_string + "0003.jpg "));
            List.Add(new Building("Academic", "Shyrock Auditorium", 37.715913, -89.218009, "Plays, speeches, and musical performances.", url_string + "0006.jpg"));
            List.Add(new Building("Academic", "Altgeld Hall", 37.716471, -89.217915, "Music", url_string + "0002.jpg"));
            List.Add(new Building("Academic", "Old Baptist Foundation", 37.716787, -89.219051, "-", url_string + "0555.jpg"));
            List.Add(new Building("Administration", "Woody Hall", 37.717676, -89.217888, "-", url_string + "0024.jpg"));
            List.Add(new Building("Academic", "Air Force ROTC", 37.719072, -89.217249, "Air Force ROTC", "http://mobile.siu.edu/MobileDawg/Maps/images/0532.jpg "));
            List.Add(new Building("Academic", "Quigley Hall", 37.717801, -89.216692, "-", url_string + "0042.jpg"));
            List.Add(new Building("Academic", "Wheeler Hall", 37.716662, -89.216712, "-", url_string + "0008.jpg"));
            List.Add(new Building("Recreation/Sports", "Davies Hall", 37.716083, -89.215989, "Home of the Saluki volleyball teams.", url_string + "0007.jpg"));
            List.Add(new Building("Recreation/Sports", "Student Recreation Center", 37.718493, -89.21227, "Olympic swimming pool, weight room, indoor track, basketball courts, racketball courts, running machines, elliptical machines, various fitness and martial arts classes.", url_string + "0149.jpg "));
            List.Add(new Building("Housing", "Wall and Grand Apartments", 37.718631, -89.209734, "Wall & Grand Apartments offer two- and four-bedroom apartments in four-story buildings on the corner of Wall Street and Grand Avenue. Each apartment houses four students.", url_string + "0301.jpg"));
            List.Add(new Building("Recreation/Sports", "Charlotte West Stadium", 37.716414, -89.213322, "Softball field named in honor of former Associate Athletic Director Dr. Charlotte West.", url_string + "0208.jpg"));
            List.Add(new Building("Housing", "Grinnell Hall", 37.715204, -89.212807, "Grinnell Hall houses the Residence Hall Dining Office, Black Togetherness Organization (BTO) Office, Residence Hall Association (RHA) Office, Eastside Express, programming space, student lounges and the mailroom for Mae Smith and Schneider Halls.", url_string + "0148.jpg"));
            List.Add(new Building("Housing", "Mae Smith Tower", 37.715391, -89.21173, "One of three 17-story towers that offer suite-style rooms, 24-hour desk service, laundry rooms on each floor and student lounges.", url_string + "0147.jpg"));
            List.Add(new Building("Housing", "Schneider Tower", 37.715412, -89.210713, "One of three 17-story towers that offer suite-style rooms, 24-hour desk service, laundry rooms on each floor and student lounges.", url_string + "0146.jpg"));
            List.Add(new Building("Housing", "Neely Hall", 37.714252, -89.212717, "One of three 17-story towers that offer suite-style rooms, 24-hour desk service, laundry rooms on each floor and student lounges.", url_string + "0142.jpg"));
            List.Add(new Building("Housing", "Trueblood Hall", 37.713379, -89.21295, "Trueblood Hall houses the Residence Life Office, dining hall, computer lab, writing center, tutoring/study space and the mailroom for Neely Hall.", url_string + "0141.jpg"));
            List.Add(new Building("Academic", "Rainbow's End", 37.719853, -89.210862, "-", url_string + "0140.jpg"));
            List.Add(new Building("Administration", "Dunn-Richmond Economic Development Center", 37.7028, -89.216494, "-", url_string + "9999.jpg"));
            List.Add(new Building("Administration", "Northwest Annex A", 37.718679, -89.223935, "IT", url_string + "0458.jpg"));
            List.Add(new Building("Administration", "Northwest Annex B", 37.718609, -89.224434, "International", url_string + "0458.jpg"));
            List.Add(new Building("Administration", "Northwest Annex C", 37.718991, -89.224044, "EAD", url_string + "0458.jpg"));
            List.Add(new Building("Administration", "Safety Center", 37.705416, -89.224121, "-", url_string + "0801.jpg"));
            List.Add(new Building("Administration", "Student Health Center", 37.718076, -89.211038, "Provides student health services.", url_string + "0269.jpg"));
            List.Add(new Building("Administration", "Student Services Building", 37.714294, -89.217063, "Undergraduate Admissions, University College, Career Services, Bursar, Financial Aid, Registrar, University Housing, Student Life, Transfer Student Services and Graduate School.", url_string + "9999.jpg"));
            List.Add(new Building("Housing", "Evergreen Terrace", 37.698605, -89.236044, "Located on West Pleasant Hill Road, on the southwest side of campus, the Evergreen Terrace complex offers two- and three-bedroom unfurnished apartments in 38 two-story buildings. Accessible apartments are available.", url_string + "0183.jpg"));
            List.Add(new Building("Housing", "University Hall", 37.715039, -89.208434, "This 4-story hall offers single (private) rooms at double room rates. Laundry rooms and community-style bathrooms are located throughout the building. Students residing in University Hall may contract for a traditional dining plan, a Block-20 dining plan or may opt out of a dining plan. A kitchenette is available on site. Alcohol is permitted in rooms where residents are age 21 and older.", url_string + "0138.jpg"));
            List.Add(new Building("Recreation/Sports", "Dorothy Morris Kumakura Garden", 37.716425, -89.219456, "The Dorothy Morris kumakura garden at Faner Hall features a bronze likeness of her that was commissioned by anonymous patrons in 2001.", url_string + "1979.jpg"));
            List.Add(new Building("Recreation/Sports", "University Courts", 37.718651, -89.217078, "University Courts houses both the men's and women's Tennis team practices and matches. There are 14 total courts on campus, some of which have been resurfaced with deco-Turf, which is the official surface of the US Open.", url_string + "9999.jpg"));
        }

        public List<Building> GetList()
        {
            return List;
        }
    }

    public class Building
    {
        private string name;
        private string category;        
        private double latitude;
        private double longitude;
        private string info;
        private string image_url;

        public Building(string c, string n, double lat, double lon, string i, string url)
        {   
            category = c;
            name = n;
            latitude = lat;
            longitude = lon;
            info = i;
            image_url = url;
        }

        public string BuildingName
        {
            get { return name; }
            set { name = value; }
        }
        public string BuildingCategory
        {
            get { return category; }
            set { category = value; }
        }
        public double BuildingLatitude
        {
            get { return latitude; }
            set { latitude = value; }
        }
        public double BuildingLongitude
        {
            get { return longitude; }
            set { longitude = value; }
        }
        public string BuildingInformation
        {
            get { return info; }
            set { info = value; }
        }
        public string BuildingImageURL
        {
            get { return image_url; }
            set { image_url = value; }
        }
    }
}
