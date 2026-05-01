# switch_accounts_get_accounts
Gets the current number of users on the consoles

## Arguments
N/A

## Syntax

```
switch_accounts_get_accounts();
```

## Returns

Number of accounts on the console, starting from 0.

## Example

```
//check if a user is active
    var accountid;
    for (var i = 0; i > switch_accounts_get_accounts(); i++;)
    {
        if (switch_accounts_is_user_open(i))
        {
            accountid = i;
            break;
        }       
    }
    show_debug_message(switch_accounts_get_nickname() + " is open!");
```