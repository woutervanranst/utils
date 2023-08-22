# WouterVanRanst.Utils

## Adding as a Git Submodule

### In a project

See https://git-scm.com/book/en/v2/Git-Tools-Submodules

Add a submodule to your main repository:
```bash
git submodule add https://github.com/woutervanranst/utils ./WouterVanRanst.Utils
```

After adding a submodule, it will be in a "pending" state. You need to initialize and update the submodule to bring in the files:
```bash
git submodule init
git submodule update
```

To clone a repository that contains submodules, you can use the --recursive flag to automatically initialize and update the submodules during the clone:
```bash
git clone --recursive <repository_url>
```

To update the submodule to the latest commit in the submodule repository:
```bash
cd <path_to_submodule_directory>
git checkout master  # or any desired branch
git pull origin master  # or the branch you checked out
cd ..
git add <path_to_submodule_directory>
git commit -m "Updated submodule to latest commit"
git push
```

! Remember to commit and push the main repository after updating the submodule, as the main repository needs to store the reference to the new submodule commit.

### In a GitHub Action

```
      - name: Check out code
        uses: actions/checkout@v2
        with:
          submodules: true
```
