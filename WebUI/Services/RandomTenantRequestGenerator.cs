namespace RentalRepairs.WebUI.Services;

/// <summary>
/// Service for generating random tenant request data for demo purposes.
/// Used in production WebUI for demo mode feature (Submit.cshtml.cs OnGetGenerateRandom endpoint).
/// Also used in tests for generating realistic test data.
/// </summary>
public class RandomTenantRequestGenerator
{
    private static readonly Random _random = new();

    private static readonly string[] _problemTypes =
    {
        "Plumbing", "Electrical", "HVAC", "Appliance", "Door/Window", "Flooring",
        "Lighting", "Security", "Pest Control", "General Maintenance", "Painting", "Roof/Ceiling"
    };

    private static readonly Dictionary<string, List<string>> _problemDescriptions = new()
    {
        ["Plumbing"] = new List<string>
        {
            "Kitchen sink is leaking water underneath the cabinet. Water is pooling on the floor and may cause damage to the cabinet bottom.",
            "Toilet in the main bathroom won't stop running water. The handle needs to be jiggled multiple times to stop the water flow.",
            "Bathroom faucet has very low water pressure. Hot water takes a very long time to come out, making it difficult to wash hands properly.",
            "Shower drain is backing up and water pools during showers. Hair and debris seem to be blocking the drain completely.",
            "Water heater is making loud banging noises, especially at night. Hot water runs out very quickly during showers.",
            "Kitchen garbage disposal stopped working and makes a humming sound when turned on. There's also a slight odor coming from it.",
            "Bathroom sink faucet is loose and wobbles when turned. Water sometimes sprays in different directions."
        },
        ["Electrical"] = new List<string>
        {
            "Several outlets in the living room have stopped working. I've checked the circuit breaker but can't identify the issue.",
            "Light switch in the bedroom is sparking when turned on or off. This seems like a safety hazard that needs immediate attention.",
            "Kitchen lights flicker randomly throughout the day. Sometimes they stay dim for hours before returning to normal brightness.",
            "Ceiling fan in the main bedroom makes a loud grinding noise and wobbles significantly when running on any speed setting.",
            "GFCI outlet in the bathroom keeps tripping and won't reset properly. Hair dryer and other appliances can't be used safely.",
            "Doorbell doesn't work and hasn't been functioning for the past two weeks. Visitors have to knock loudly to be heard.",
            "Outdoor porch light stays on constantly and can't be turned off from the inside switch. This is wasting electricity."
        },
        ["HVAC"] = new List<string>
        {
            "Air conditioning unit is making very loud noises and doesn't seem to be cooling effectively. The air coming out feels warm.",
            "Heater isn't working and the apartment is getting very cold. Thermostat shows the right temperature but no heat is coming through vents.",
            "Thermostat display is blank and unresponsive. Can't control the temperature and the unit seems to run constantly.",
            "Air vents in the bedroom are blowing dusty air and have a musty odor. This might be affecting air quality and allergies.",
            "Central air unit outside the building is making grinding noises and vibrating excessively. Neighbors have also complained.",
            "Bathroom exhaust fan is very loud and doesn't seem to be removing moisture effectively. The bathroom stays steamy for hours.",
            "Heating vents in the living room are barely blowing air. The room stays much colder than the rest of the apartment."
        },
        ["Appliance"] = new List<string>
        {
            "Refrigerator is making constant clicking and humming noises. The freezer isn't maintaining proper temperature and ice cream is melting.",
            "Dishwasher leaks water onto the kitchen floor after every cycle. The leak appears to come from the bottom front area.",
            "Washing machine in the unit doesn't complete cycles properly and leaves clothes soaking wet. The spin cycle seems to be malfunctioning.",
            "Dryer not working properly and takes multiple cycles to dry clothes. Produces very little heat and lint filter has been cleaned but issue persists.",
            "Oven temperature is inconsistent and food burns on one side while remaining undercooked on the other. Temperature dial seems inaccurate.",
            "Microwave door doesn't close properly and the unit won't start. The latch mechanism appears to be broken or misaligned.",
            "Range hood light bulb burned out and the exhaust fan makes a loud rattling noise when turned on."
        },
        ["Door/Window"] = new List<string>
        {
            "Front door lock is very difficult to turn and sometimes gets stuck. Have to use significant force to lock or unlock the door.",
            "Bedroom window won't open and appears to be painted shut. This is a safety concern for emergency exit purposes.",
            "Sliding patio door is very hard to open and close. The track seems damaged and the door sometimes jumps off the rails.",
            "Bathroom door handle is loose and the door won't stay closed properly. Privacy is an issue and the handle may fall off completely.",
            "Kitchen window has a large crack in the glass that appeared after the last storm. Cold air is coming through the crack.",
            "Closet door in the bedroom has come off its hinges and is leaning against the wall. Can't access clothing properly.",
            "Screen door has several large tears and doesn't keep insects out effectively. The frame is also bent in one corner."
        },
        ["Flooring"] = new List<string>
        {
            "Hardwood floor in the living room has several loose boards that creak loudly and move when stepped on. May be a safety hazard.",
            "Carpet in the bedroom has a large stain that won't come out with regular cleaning. The stain has been there since move-in.",
            "Tile in the bathroom is cracked and some pieces are coming loose. Water may be seeping underneath causing damage.",
            "Vinyl flooring in the kitchen is peeling at the edges and has several tears. Food and liquids get trapped underneath.",
            "Laminate flooring in the hallway is buckling and has gaps between planks. The floor feels uneven when walking.",
            "Bathroom floor feels soft and spongy near the toilet. There may be water damage to the subfloor underneath.",
            "Living room carpet has several burn marks and the padding underneath feels compressed and uncomfortable."
        },
        ["Lighting"] = new List<string>
        {
            "Light fixture in the dining room hangs crooked and sways when the upstairs neighbors walk around. Mounting may be loose.",
            "Fluorescent light in the kitchen buzzes loudly and flickers constantly. The ballast may need replacement.",
            "Bathroom vanity lights have burned out bulbs but replacing them doesn't fix the issue. May be an electrical problem.",
            "Ceiling light in the bedroom doesn't turn on despite replacing bulbs multiple times. Switch seems to work but no power reaches fixture.",
            "Track lighting in the living room has several spots that don't work. Only about half of the lights function properly.",
            "Outdoor walkway light is broken and creates a safety hazard for entering the building at night. Glass cover is also cracked.",
            "Closet light pull-chain broke and the light is stuck in the on position. Can't turn it off which wastes electricity."
        },
        ["Security"] = new List<string>
        {
            "Deadbolt lock on the front door is difficult to engage and sometimes doesn't lock completely. Security is compromised.",
            "Window locks in the bedroom are broken and windows can be opened from outside. This is a serious security concern.",
            "Peephole in the front door is cloudy and it's impossible to see who is outside clearly. Need to replace for safety.",
            "Sliding door lock doesn't engage properly and the door can be forced open with minimal effort. Very concerning for security.",
            "Mailbox lock is broken and mail is not secure. Important documents and packages could be stolen easily.",
            "Balcony door can be opened from outside despite being locked. The locking mechanism appears to be faulty.",
            "Entry gate code reader is malfunctioning and visitors can't be buzzed in properly. Having to meet everyone at the gate."
        },
        ["Pest Control"] = new List<string>
        {
            "Ants are entering the kitchen through cracks near the window and gathering around food prep areas. Treatment needed urgently.",
            "Cockroaches spotted in the bathroom and kitchen, especially at night. This is a health concern that needs professional attention.",
            "Mice droppings found in kitchen cabinets and pantry. Heard scratching noises in walls at night. Need immediate pest control.",
            "Wasps have built a nest under the balcony eaves and are becoming aggressive. Unsafe to use outdoor space.",
            "Spider webs constantly appearing in corners of rooms despite regular cleaning. Large spiders seen frequently.",
            "Fruit flies swarming in the kitchen despite keeping area clean. Issue persists even with removal of all fruit.",
            "Strange buzzing noise coming from within the walls. May be bees or other insects nesting inside and needs repair work."
        },
        ["General Maintenance"] = new List<string>
        {
            "Smoke detector battery is low and beeping every few minutes throughout the day and night. Very disruptive to sleep.",
            "Caulking around bathtub is moldy and peeling away from the wall. Water is getting behind the tub causing potential damage.",
            "Paint is peeling from bathroom ceiling, likely due to moisture issues. Small pieces fall occasionally.",
            "Baseboards in the living room are coming away from the wall and have gaps that collect dust and debris.",
            "Weather stripping around windows and doors is worn and cold air enters freely. Heating bills are higher than expected.",
            "Grout between bathroom tiles is cracked and discolored. Water seeps through and there's a musty smell.",
            "Cabinet doors in the kitchen don't close properly and some hinges are completely broken. Storage is difficult."
        },
        ["Painting"] = new List<string>
        {
            "Paint on the living room walls is chipped and peeling in several large areas. The wall surface is becoming rough and unsightly.",
            "Bathroom paint is bubbling and peeling due to moisture. Some areas have exposed drywall that needs attention.",
            "Bedroom wall has large scuff marks and holes from previous tenant that need to be patched and painted over.",
            "Kitchen paint near the stove is discolored from grease and heat. The surface is also becoming sticky to touch.",
            "Hallway walls have numerous nail holes and tape marks from previous decorations that need professional repair.",
            "Ceiling paint in the living room is yellowing and has water stains from what appears to be an old leak.",
            "Exterior door paint is fading and chipping, exposing bare wood underneath. Weather protection is compromised."
        },
        ["Roof/Ceiling"] = new List<string>
        {
            "Water stain on the bedroom ceiling that keeps growing after each rain. There may be an active roof leak above.",
            "Ceiling fan mounting appears loose and the fan wobbles dangerously when running. Safety concern for people below.",
            "Popcorn ceiling in the living room is flaking and small pieces fall regularly. May contain asbestos and needs testing.",
            "Skylight in the bathroom has condensation buildup and water sometimes drips onto the floor during storms.",
            "Ceiling tiles in the kitchen are sagging and discolored. May indicate moisture problems or structural issues above.",
            "Attic access panel in the hallway ceiling is loose and sometimes opens partially on its own. Insulation falls out occasionally.",
            "Light fixture mounting in the dining room ceiling appears cracked around the edges. Fixture may fall if not repaired."
        }
    };


    private static readonly string[] _contactTimes =
    {
        "Morning (8 AM - 12 PM)",
        "Afternoon (12 PM - 5 PM)",
        "Evening (5 PM - 8 PM)",
        "Anytime"
    };

    /// <summary>
    /// Generate a random tenant request with realistic problem descriptions
    /// </summary>
    public static RandomRequestData GenerateRandomRequest()
    {
        var problemType = _problemTypes[_random.Next(_problemTypes.Length)];
        var descriptions = _problemDescriptions[problemType];
        var description = descriptions[_random.Next(descriptions.Count)];

        // Occasionally add additional context to make it more realistic
        if (_random.Next(100) < 30) // 30% chance
        {
            var additionalContext = new[]
            {
                " This issue started about a week ago.",
                " The problem has been getting worse over the past few days.",
                " This is affecting my daily routine significantly.",
                " I've tried basic troubleshooting but the issue persists.",
                " Previous tenant mentioned this was an ongoing issue.",
                " This needs to be fixed before it becomes a bigger problem.",
                " I'm available most days for repair scheduling.",
                " Please let me know if you need additional details."
            };
            description += additionalContext[_random.Next(additionalContext.Length)];
        }

        // Adjust urgency based on problem type
        var urgency = GetUrgencyForProblemType(problemType);

        return new RandomRequestData
        {
            ProblemDescription = description,
            UrgencyLevel = urgency,
            PreferredContactTime = _contactTimes[_random.Next(_contactTimes.Length)]
        };
    }

    /// <summary>
    /// Get appropriate urgency level based on problem type
    /// </summary>
    private static string GetUrgencyForProblemType(string problemType)
    {
        return problemType switch
        {
            "Electrical" => _random.Next(100) < 40 ? "High" : "Normal", // Electrical issues often urgent
            "Plumbing" => _random.Next(100) < 30 ? "High" : "Normal", // Plumbing can be urgent
            "Security" => _random.Next(100) < 50 ? "High" : "Normal", // Security issues important
            "HVAC" => _random.Next(100) < 25 ? "High" : "Normal", // HVAC less often urgent
            "Pest Control" => _random.Next(100) < 35 ? "High" : "Normal", // Pests can be urgent
            "Roof/Ceiling" => _random.Next(100) < 20 ? "Critical" :
                _random.Next(100) < 40 ? "High" : "Normal", // Leaks can be critical
            _ => _random.Next(100) < 10 ? "High" : _random.Next(100) < 70 ? "Normal" : "Low" // General distribution
        };
    }

    /// <summary>
    /// Generate multiple random requests for testing
    /// </summary>
    public static List<RandomRequestData> GenerateMultipleRequests(int count)
    {
        var requests = new List<RandomRequestData>();
        for (var i = 0; i < count; i++) requests.Add(GenerateRandomRequest());
        return requests;
    }
}

/// <summary>
/// Data model for generated random request
/// </summary>
public class RandomRequestData
{
    public string ProblemDescription { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public string PreferredContactTime { get; set; } = string.Empty;
}