using Spectre.Console;
using DetectiveConanRenamer.Interfaces;
using DetectiveConanRenamer.Models;
using DetectiveConanRenamer.Utils;

namespace DetectiveConanRenamer.Services
{
    public interface IMenuService
    {
        Task ShowMainMenu();
        Task ShowRenameEpisodesMenu();
        Task ShowScrapeMenu();
        Task ShowConfigMenu();
        Task ShowTestMenu();
        Task ShowRegexMenu();
    }

    public class MenuService : IMenuService
    {
        private readonly IEpisodeService _episodeService;
        private readonly IFileRenamer _fileRenamer;
        private readonly IWikiScraperService _wikiScraper;
        private readonly ILoggingService _loggingService;
        private readonly IRegexPatternService _regexPatternService;
        private readonly IBackupService _backupService;

        public MenuService(
            IEpisodeService episodeService,
            IFileRenamer fileRenamer,
            IWikiScraperService wikiScraper,
            ILoggingService loggingService,
            IBackupService backupService,
            IRegexPatternService regexPatternService)
        {
            _episodeService = episodeService;
            _fileRenamer = fileRenamer;
            _wikiScraper = wikiScraper;
            _loggingService = loggingService;
            _backupService = backupService;
            _regexPatternService = regexPatternService;
        }

        public async Task ShowMainMenu()
        {
            while (true)
            {
                Console.Clear();
                var title = new FigletText("Detective Conan")
                    .Centered()
                    .Color(Color.Aqua);

                var header = new Panel(title)
                    .Header("Renommeur d'épisodes")
                    .BorderColor(Color.Aqua)
                    .Border(BoxBorder.Rounded)
                    .Padding(2, 1);

                AnsiConsole.Write(header);

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[aqua]Que souhaitez-vous faire ?[/]")
                        .PageSize(10)
                        .HighlightStyle(new Style(foreground: Color.Aqua))
                        .AddChoices(new[] {
                            "Renommer les épisodes",
                            "Scraper les titres",
                            "Gérer les patterns regex",
                            "Configuration",
                            "Tests",
                            "Quitter"
                        }));

                switch (choice)
                {
                    case "Renommer les épisodes":
                        await ShowRenameEpisodesMenu();
                        break;
                    case "Scraper les titres":
                        await ShowScrapeMenu();
                        break;
                    case "Gérer les patterns regex":
                        await ShowRegexMenu();
                        break;
                    case "Configuration":
                        await ShowConfigMenu();
                        break;
                    case "Tests":
                        await ShowTestMenu();
                        break;
                    case "Quitter":
                        return;
                }
            }
        }

        public async Task ShowRenameEpisodesMenu()
        {
            Console.Clear();
            var title = new FigletText("Renommage")
                .Centered()
                .Color(Color.Green);

            var header = new Panel(title)
                .Header("Renommage des épisodes")
                .BorderColor(Color.Green)
                .Border(BoxBorder.Rounded)
                .Padding(2, 1);

            AnsiConsole.Write(header);

            var episodes = await _episodeService.GetEpisodesAsync();
            if (episodes.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Aucun titre d'épisode trouvé. Veuillez d'abord scraper les titres.[/]");
                AnsiConsole.MarkupLine("Appuyez sur une touche pour continuer...");
                Console.ReadKey();
                return;
            }

            var directoryPath = AnsiConsole.Ask<string>("[green]Chemin du dossier contenant les épisodes :[/]");
            if (!Directory.Exists(directoryPath))
            {
                AnsiConsole.MarkupLine("[red]Le dossier n'existe pas ![/]");
                AnsiConsole.MarkupLine("Appuyez sur une touche pour continuer...");
                Console.ReadKey();
                return;
            }

            var files = Directory.GetFiles(directoryPath)
                .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || 
                           f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (files.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Aucun fichier vidéo (.mp4 ou .mkv) trouvé dans le dossier ![/]");
                AnsiConsole.MarkupLine("Appuyez sur une touche pour continuer...");
                Console.ReadKey();
                return;
            }

            AnsiConsole.MarkupLine($"[green]Trouvé {files.Count} fichiers vidéo et {episodes.Count} titres disponibles[/]");
            
            var createBackup = AnsiConsole.Confirm("[yellow]Voulez-vous créer une sauvegarde avant le renommage ? (Cette opération peut prendre du temps en fonction de la taille des fichiers et de la vitesse du disque dur.)[/]");
          
            if (createBackup)
            {
                AnsiConsole.MarkupLine("[yellow]Création des sauvegardes...[/]");
                await AnsiConsole.Progress()
                    .AutoClear(false)
                    .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn(),
                    })
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[green]Sauvegarde des fichiers[/]");
                        task.MaxValue = files.Count;
                        
                        foreach (var file in files)
                        {
                            try
                            {
                                await _backupService.CreateBackupAsync(file);
                                AnsiConsole.MarkupLine($"[green]Sauvegarde créée pour : {Path.GetFileName(file)}[/]");
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Erreur lors de la sauvegarde de {Path.GetFileName(file)} : {ex.Message}[/]");
                            }
                            
                            task.Increment(1);
                        }
                    });
                
                AnsiConsole.MarkupLine("[green]Sauvegardes terminées ![/]");
            }

            AnsiConsole.MarkupLine("Appuyez sur une touche pour commencer le renommage...");
            Console.ReadKey();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Renommage des fichiers[/]");
                    task.MaxValue = files.Count;
                    
                    _fileRenamer.RenameFiles(directoryPath, episodes);
                    task.Increment(files.Count);
                });

            AnsiConsole.MarkupLine("[green]Renommage terminé ![/]");
            AnsiConsole.MarkupLine("Appuyez sur une touche pour continuer...");
            Console.ReadKey();
        }

        public async Task ShowScrapeMenu()
        {
            Console.Clear();
            var title = new FigletText("Scraping")
                .Centered()
                .Color(Color.Yellow);

            var header = new Panel(title)
                .Header("Scraping des titres")
                .BorderColor(Color.Yellow)
                .Border(BoxBorder.Rounded)
                .Padding(2, 1);

            AnsiConsole.Write(header);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Que souhaitez-vous faire ?[/]")
                    .PageSize(10)
                    .HighlightStyle(new Style(foreground: Color.Yellow))
                    .AddChoices(new[] {
                        "Scraper toutes les saisons",
                        "Scraper une saison spécifique",
                        "Retour"
                    }));

            switch (choice)
            {
                case "Scraper toutes les saisons":
                    await AnsiConsole.Progress()
                        .AutoClear(false)
                        .Columns(new ProgressColumn[]
                        {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new SpinnerColumn(),
                        })
                        .StartAsync(async ctx =>
                        {
                            var task = ctx.AddTask("[yellow]Scraping de toutes les saisons[/]");
                            task.MaxValue = 30; // Nombre total de saisons
                            
                            await _wikiScraper.ScrapeAllSeasonsAsync();
                            task.Increment(30);
                        });
                    break;

                case "Scraper une saison spécifique":
                    var seasonNumber = AnsiConsole.Prompt(
                        new TextPrompt<int>("[yellow]Numéro de la saison (1-30) :[/]")
                            .ValidationErrorMessage("[red]Numéro de saison invalide[/]")
                            .Validate(number =>
                            {
                                return number switch
                                {
                                    <= 0 => ValidationResult.Error("Le numéro doit être supérieur à 0"),
                                    > 30 => ValidationResult.Error("Le numéro doit être inférieur ou égal à 30"),
                                    _ => ValidationResult.Success()
                                };
                            }));

                    await AnsiConsole.Progress()
                        .AutoClear(false)
                        .Columns(new ProgressColumn[]
                        {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new SpinnerColumn(),
                        })
                        .StartAsync(async ctx =>
                        {
                            var task = ctx.AddTask($"[yellow]Scraping de la saison {seasonNumber}[/]");
                            task.MaxValue = 1;
                            
                            await _wikiScraper.ScrapeSeasonAsync(seasonNumber);
                            task.Increment(1);
                        });
                    break;
            }

            // Recharger les épisodes après le scraping
            await _episodeService.ReloadEpisodesAsync();

            AnsiConsole.MarkupLine("[green]Scraping terminé ![/]");
            AnsiConsole.MarkupLine("Appuyez sur une touche pour continuer...");
            Console.ReadKey();
        }

        public async Task ShowConfigMenu()
        {
            Console.Clear();
            var title = new FigletText("Config")
                .Centered()
                .Color(Color.Blue);

            var header = new Panel(title)
                .Header("Configuration")
                .BorderColor(Color.Blue)
                .Border(BoxBorder.Rounded)
                .Padding(2, 1);

            AnsiConsole.Write(header);

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .AddColumn(new TableColumn("Paramètre").Centered())
                .AddColumn(new TableColumn("Valeur").Centered());

            table.AddRow("Dossier des données", AppSettings.DataDirectory);
            table.AddRow("Fichier des épisodes", AppSettings.EpisodesFileName);
            table.AddRow("Dossier de sauvegarde", AppSettings.BackupDirectory);

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[blue]Que souhaitez-vous faire ?[/]")
                    .PageSize(10)
                    .HighlightStyle(new Style(foreground: Color.Blue))
                    .AddChoices(new[] {
                        "Modifier le dossier des données",
                        "Modifier le fichier des épisodes",
                        "Modifier le dossier de sauvegarde",
                        "Retour"
                    }));

            switch (choice)
            {
                case "Modifier le dossier des données":
                    var newDataDir = AnsiConsole.Ask<string>("[blue]Nouveau chemin du dossier des données :[/]", AppSettings.DataDirectory);
                    try
                    {
                        AppSettings.SetDataDirectory(newDataDir);
                        AnsiConsole.MarkupLine("[green]Dossier des données modifié avec succès ![/]");
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                    }
                    break;

                case "Modifier le fichier des épisodes":
                    var newEpisodesFile = AnsiConsole.Ask<string>("[blue]Nouveau nom du fichier des épisodes :[/]", AppSettings.EpisodesFileName);
                    try
                    {
                        AppSettings.SetEpisodesFileName(newEpisodesFile);
                        AnsiConsole.MarkupLine("[green]Fichier des épisodes modifié avec succès ![/]");
                    }
                    catch (ArgumentException ex)
                    {
                        AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                    }
                    break;

                case "Modifier le dossier de sauvegarde":
                    var newBackupDir = AnsiConsole.Ask<string>("[blue]Nouveau chemin du dossier de sauvegarde :[/]", AppSettings.BackupDirectory);
                    try
                    {
                        AppSettings.SetBackupDirectory(newBackupDir);
                        AnsiConsole.MarkupLine("[green]Dossier de sauvegarde modifié avec succès ![/]");
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                    }
                    break;
            }

            AnsiConsole.MarkupLine("Appuyez sur une touche pour continuer...");
            Console.ReadKey();
        }

        public async Task ShowTestMenu()
        {
            await Task.Run(() =>
            {
                Console.Clear();
                var title = new FigletText("Tests")
                    .Centered()
                    .Color(Color.Purple);

                var header = new Panel(title)
                    .Header("Tests")
                    .BorderColor(Color.Purple)
                    .Border(BoxBorder.Rounded)
                    .Padding(2, 1);

                AnsiConsole.Write(header);
            });

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[purple]Que souhaitez-vous tester ?[/]")
                    .PageSize(10)
                    .HighlightStyle(new Style(foreground: Color.Purple))
                    .AddChoices(new[] {
                        "Tester un pattern regex",
                        "Tester le renommage",
                        "Tester le scraping",
                        "Retour"
                    }));

            switch (choice)
            {
                case "Tester un pattern regex":
                    var patterns = await _regexPatternService.GetPatternsAsync();
                    var patternNames = patterns.Select(p => p.Name).ToList();
                    patternNames.Add("Retour");

                    var selectedPatternName = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[purple]Sélectionnez un pattern :[/]")
                            .PageSize(10)
                            .HighlightStyle(new Style(foreground: Color.Purple))
                            .AddChoices(patternNames));

                    if (selectedPatternName == "Retour")
                        break;

                    var selectedPattern = patterns.First(p => p.Name == selectedPatternName);
                    var fileName = AnsiConsole.Ask<string>("[purple]Entrez un nom de fichier à tester :[/]");

                    var episodeNumber = await _regexPatternService.ExtractEpisodeNumberAsync(fileName);
                    if (episodeNumber.HasValue)
                    {
                        AnsiConsole.MarkupLine($"[green]Numéro d'épisode trouvé : {episodeNumber}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Aucun numéro d'épisode trouvé[/]");
                    }
                    break;

                case "Tester le renommage":
                    var testDir = AnsiConsole.Ask<string>("[purple]Chemin du dossier de test :[/]");
                    if (!Directory.Exists(testDir))
                    {
                        AnsiConsole.MarkupLine("[red]Le dossier n'existe pas ![/]");
                        break;
                    }

                    var testFiles = Directory.GetFiles(testDir)
                        .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || 
                                   f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (testFiles.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[red]Aucun fichier vidéo (.mp4 ou .mkv) trouvé dans le dossier ![/]");
                        break;
                    }

                    AnsiConsole.MarkupLine($"[green]Trouvé {testFiles.Count} fichiers vidéo[/]");
                    
                    var testEpisodes = await _episodeService.GetEpisodesAsync();
                    if (testEpisodes.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[red]Aucun titre d'épisode trouvé. Veuillez d'abord scraper les titres.[/]");
                        break;
                    }

                    AnsiConsole.MarkupLine("[yellow]Simulation du renommage (aucun fichier ne sera modifié) :[/]");
                    
                    var renameTable = new Table()
                        .Border(TableBorder.Rounded)
                        .BorderColor(Color.Purple)
                        .AddColumn(new TableColumn("Fichier original").Centered())
                        .AddColumn(new TableColumn("Nouveau nom").Centered());

                    foreach (var file in testFiles)
                    {
                        var testFileName = Path.GetFileName(file);
                        var testEpisodeNumber = await _regexPatternService.ExtractEpisodeNumberAsync(testFileName);
                        
                        if (testEpisodeNumber.HasValue && testEpisodes.ContainsKey(testEpisodeNumber.Value))
                        {
                            var episodeTitle = testEpisodes[testEpisodeNumber.Value];
                            var newFileName = $"{testEpisodeNumber.Value:D3} - {episodeTitle}.{Path.GetExtension(testFileName)}";
                            renameTable.AddRow(testFileName, newFileName);
                        }
                        else
                        {
                            renameTable.AddRow(testFileName, "[red]Impossible de déterminer le numéro d'épisode[/]");
                        }
                    }

                    AnsiConsole.Write(renameTable);
                    break;

                case "Tester le scraping":
                    var seasonToTest = AnsiConsole.Prompt(
                        new TextPrompt<int>("[purple]Numéro de la saison à tester (1-30) :[/]")
                            .ValidationErrorMessage("[red]Numéro de saison invalide[/]")
                            .Validate(number =>
                            {
                                return number switch
                                {
                                    <= 0 => ValidationResult.Error("Le numéro doit être supérieur à 0"),
                                    > 30 => ValidationResult.Error("Le numéro doit être inférieur ou égal à 30"),
                                    _ => ValidationResult.Success()
                                };
                            }));

                    AnsiConsole.MarkupLine($"[yellow]Test du scraping de la saison {seasonToTest}...[/]");
                    
                    try
                    {
                        await _wikiScraper.ScrapeSeasonAsync(seasonToTest);
                        
                        // Récupérer les épisodes après le scraping
                        var scrapedEpisodes = await _episodeService.GetEpisodesAsync();
                        
                        if (scrapedEpisodes.Count > 0)
                        {
                            AnsiConsole.MarkupLine($"[green]Scraping réussi ! {scrapedEpisodes.Count} épisodes trouvés.[/]");
                            
                            var scrapeTable = new Table()
                                .Border(TableBorder.Rounded)
                                .BorderColor(Color.Purple)
                                .AddColumn(new TableColumn("Numéro").Centered())
                                .AddColumn(new TableColumn("Titre").Centered());
                                
                            foreach (var episode in scrapedEpisodes.OrderBy(e => e.Key))
                            {
                                scrapeTable.AddRow(episode.Key.ToString(), episode.Value);
                            }
                            
                            AnsiConsole.Write(scrapeTable);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]Aucun épisode trouvé pour cette saison.[/]");
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Erreur lors du scraping : {ex.Message}[/]");
                    }
                    break;
            }

            AnsiConsole.MarkupLine("Appuyez sur une touche pour continuer...");
            Console.ReadKey();
        }

        public async Task ShowRegexMenu()
        {
            Console.Clear();
            var title = new FigletText("Regex")
                .Centered()
                .Color(Color.Orange3);

            var header = new Panel(title)
                .Header("Gestion des patterns regex")
                .BorderColor(Color.Orange3)
                .Border(BoxBorder.Rounded)
                .Padding(2, 1);

            AnsiConsole.Write(header);

            while (true)
            {
                var patterns = await _regexPatternService.GetPatternsAsync();
                
                // Afficher les patterns dans un tableau
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Orange3)
                    .AddColumn(new TableColumn("Nom").Centered())
                    .AddColumn(new TableColumn("Pattern").Centered())
                    .AddColumn(new TableColumn("Description").Centered())
                    .AddColumn(new TableColumn("Groupe").Centered())
                    .AddColumn(new TableColumn("Par défaut").Centered())
                    .AddColumn(new TableColumn("État").Centered());

                foreach (var pattern in patterns)
                {
                    table.AddRow(
                        pattern.Name,
                        pattern.Pattern,
                        pattern.Description,
                        pattern.CaptureGroup.ToString(),
                        pattern.IsDefault ? "[green]Oui[/]" : "[red]Non[/]",
                        pattern.IsEnabled ? "[green]Activé[/]" : "[red]Désactivé[/]"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[orange3]Que souhaitez-vous faire ?[/]")
                        .PageSize(10)
                        .HighlightStyle(new Style(foreground: Color.Orange3))
                        .AddChoices(new[] {
                            "Ajouter un pattern",
                            "Modifier un pattern",
                            "Supprimer un pattern",
                            "Activer/Désactiver un pattern",
                            "Tester un pattern",
                            "Retour"
                        }));

                switch (choice)
                {
                    case "Ajouter un pattern":
                        var newPattern = new RegexPattern
                        {
                            Name = AnsiConsole.Ask<string>("[orange3]Nom du pattern :[/]"),
                            Pattern = AnsiConsole.Ask<string>("[orange3]Pattern regex :[/]"),
                            Description = AnsiConsole.Ask<string>("[orange3]Description :[/]"),
                            CaptureGroup = AnsiConsole.Prompt(
                                new TextPrompt<int>("[orange3]Groupe de capture (1 par défaut) :[/]")
                                    .DefaultValue(1)),
                            IsDefault = AnsiConsole.Confirm("[orange3]Pattern par défaut ?[/]"),
                            IsEnabled = true
                        };

                        patterns.Add(newPattern);
                        await _regexPatternService.SavePatternsAsync(patterns);
                        AnsiConsole.MarkupLine("[green]Pattern ajouté avec succès ![/]");
                        break;

                    case "Modifier un pattern":
                        var patternToEdit = AnsiConsole.Prompt(
                            new SelectionPrompt<RegexPattern>()
                                .Title("[orange3]Sélectionnez un pattern à modifier :[/]")
                                .PageSize(10)
                                .HighlightStyle(new Style(foreground: Color.Orange3))
                                .UseConverter(pattern => pattern.Name)
                                .AddChoices(patterns));

                        patternToEdit.Name = AnsiConsole.Ask<string>("[orange3]Nouveau nom :[/]", patternToEdit.Name);
                        patternToEdit.Pattern = AnsiConsole.Ask<string>("[orange3]Nouveau pattern :[/]", patternToEdit.Pattern);
                        patternToEdit.Description = AnsiConsole.Ask<string>("[orange3]Nouvelle description :[/]", patternToEdit.Description);
                        patternToEdit.CaptureGroup = AnsiConsole.Prompt(
                            new TextPrompt<int>("[orange3]Nouveau groupe de capture :[/]")
                                .DefaultValue(patternToEdit.CaptureGroup));
                        patternToEdit.IsDefault = AnsiConsole.Confirm("[orange3]Pattern par défaut ?[/]", patternToEdit.IsDefault);

                        await _regexPatternService.SavePatternsAsync(patterns);
                        AnsiConsole.MarkupLine("[green]Pattern modifié avec succès ![/]");
                        break;

                    case "Supprimer un pattern":
                        var patternToDelete = AnsiConsole.Prompt(
                            new SelectionPrompt<RegexPattern>()
                                .Title("[orange3]Sélectionnez un pattern à supprimer :[/]")
                                .PageSize(10)
                                .HighlightStyle(new Style(foreground: Color.Orange3))
                                .UseConverter(pattern => pattern.Name)
                                .AddChoices(patterns));

                        if (AnsiConsole.Confirm($"[red]Êtes-vous sûr de vouloir supprimer le pattern {patternToDelete.Name} ?[/]"))
                        {
                            patterns.Remove(patternToDelete);
                            await _regexPatternService.SavePatternsAsync(patterns);
                            AnsiConsole.MarkupLine("[green]Pattern supprimé avec succès ![/]");
                        }
                        break;

                    case "Activer/Désactiver un pattern":
                        var patternToToggle = AnsiConsole.Prompt(
                            new SelectionPrompt<RegexPattern>()
                                .Title("[orange3]Sélectionnez un pattern à activer/désactiver :[/]")
                                .PageSize(10)
                                .HighlightStyle(new Style(foreground: Color.Orange3))
                                .UseConverter(pattern => pattern.Name)
                                .AddChoices(patterns));

                        var currentState = patternToToggle.IsEnabled;
                        var newState = AnsiConsole.Confirm($"[orange3]Voulez-vous {(currentState ? "désactiver" : "activer")} le pattern {patternToToggle.Name} ?[/]", !currentState);
                        patternToToggle.IsEnabled = newState;
                        await _regexPatternService.SavePatternsAsync(patterns);
                        AnsiConsole.MarkupLine($"[green]Pattern {patternToToggle.Name} {(patternToToggle.IsEnabled ? "activé" : "désactivé")} avec succès ![/]");
                        break;

                    case "Tester un pattern":
                        var patternToTest = AnsiConsole.Prompt(
                            new SelectionPrompt<RegexPattern>()
                                .Title("[orange3]Sélectionnez un pattern à tester :[/]")
                                .PageSize(10)
                                .HighlightStyle(new Style(foreground: Color.Orange3))
                                .UseConverter(pattern => pattern.Name)
                                .AddChoices(patterns));

                        var fileName = AnsiConsole.Ask<string>("[orange3]Entrez un nom de fichier à tester :[/]");
                        
                        var regex = patternToTest.ToRegex();
                        var match = regex.Match(fileName);
                        
                        if (match.Success)
                        {
                            AnsiConsole.MarkupLine("[green]Match trouvé ![/]");
                            
                            var matchTable = new Table()
                                .Border(TableBorder.Rounded)
                                .BorderColor(Color.Green)
                                .AddColumn(new TableColumn("Groupe").Centered())
                                .AddColumn(new TableColumn("Valeur").Centered());
                                
                            for (int i = 0; i < match.Groups.Count; i++)
                            {
                                matchTable.AddRow(i.ToString(), match.Groups[i].Value);
                            }
                            
                            AnsiConsole.Write(matchTable);
                            
                            if (match.Groups.Count > patternToTest.CaptureGroup)
                            {
                                var group = match.Groups[patternToTest.CaptureGroup];
                                if (int.TryParse(group.Value, out int episodeNumber))
                                {
                                    AnsiConsole.MarkupLine($"[green]Numéro d'épisode extrait : {episodeNumber}[/]");
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine("[yellow]Le groupe de capture ne contient pas un numéro d'épisode valide.[/]");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine("[yellow]Le groupe de capture spécifié n'existe pas dans le match.[/]");
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]Aucun match trouvé.[/]");
                        }
                        break;

                    case "Retour":
                        return;
                }

                AnsiConsole.MarkupLine("Appuyez sur une touche pour continuer...");
                Console.ReadKey();
                Console.Clear();
                AnsiConsole.Write(header);
            }
        }
    }
} 