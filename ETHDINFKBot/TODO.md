- store in a config most needed channels and have them available globaly (since they should almost never change)

- generalize automatic reactions
    - upvote/downvote
    - awww

- split up reaction handling
    - save reaction

- split message handling
    - admin bait

- cron jobs
    - backup db
    - cleanup server suggestions
    - preload old messages to db (4am job)

- add command to see all perms for all channels
- add posibility to edit channel perms from outside

- finish preload messages -> as cron job
- add preload table info

- eth room info
    - add new tables
    - add sync logic
    - add draw logic
    - add recognize room image location
    - add free room draewing
    - add free room draw
    - save floor plan (maybe depending on space and amount)

- introduce propper branches for update

- break up database manager 
    - folder database
        - file per "entity"

- remove old unused code

- daily, weekly, monthly (and maybe yearly statistics) for server
-   maybe (only after preload is run) message for every 100'000th message or so
- callable stats 


- fix ping history
- complete ghost ping command
- fix leaderboard
- personalised cool graphs with dynamic "stuff"
- leaderboard for most daily first messages
- break up discord module into multiple other modules
- find a way to auto create help page by using tags and such

- add way to remove suggestions if admin user reacts to it
- clean pull request channel on success or deny reaction
- add logic to remove highly downvoted suggestions

- fix bot crashes :pepegun:

- slow mode info for admins if they again forget 

- drawing
    - move sql drawing to the drawing engine
    - add point chart
    - add line chart
    - add bar chart
    - add pie chart
    - add x as int val handling
    - better x axis labeling

- exam
    - add subject, exam, question tables
    - add a way to add new questions to the bot or edit them
    - add spoilers to the question



- add a new column to discord emotes for index -> faster select
    - option to rank emotes so the "good ones" receive index 0

- html/pdf help page (incl all bots)

- emote min edit distance to find better match if no emote exists
    - maybe personalised alias?


- add option to delete dm's with saved posts


- add last poster of the day


